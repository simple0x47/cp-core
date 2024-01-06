using System.Net;
using ICSharpCode.SharpZipLib.Zip;

namespace Core.Config;

public class ServerConfigDownloader : IConfigDownloader
{
    private readonly IHttpClient _client;

    public ServerConfigDownloader(IHttpClient client)
    {
        _client = client;
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    public async Task<Result<string, Error>> Download(string url, string component)
    {
        try
        {
            url = $"{url}/get/{component}";
            string targetDirectory = $"{Directory.GetCurrentDirectory()}/{new Guid().ToString()}";
            Directory.CreateDirectory(targetDirectory);

            Result<Empty, Error> downloadResult = await DownloadConfig(url, targetDirectory);

            if (!downloadResult.IsOk) return Result<string, Error>.Err(downloadResult.UnwrapErr());

            return Result<string, Error>.Ok(targetDirectory);
        }
        catch (Exception e)
        {
            return Result<string, Error>.Err(new Error(ErrorKind.ExceptionThrown,
                $"an exception '{e.GetType().Name}' has been thrown: {e.Message}: {e.StackTrace}"));
        }
    }

    private async Task<Result<Empty, Error>> DownloadConfig(string url, string targetDirectory)
    {
        string packageFile = $"{new Guid().ToString()}.zip";

        try
        {
            using HttpResponseMessage response = await _client.GetAsync(url);

            if (response.StatusCode != HttpStatusCode.OK)
                return Result<Empty, Error>.Err(new Error(ErrorKind.DownloadFailure,
                    $"failed to download zip file containing configuration with url: {url}"));

            byte[] configZipData = await response.Content.ReadAsByteArrayAsync();
            await File.WriteAllBytesAsync(packageFile, configZipData);

            FastZip fastZip = new();
            fastZip.ExtractZip(packageFile, targetDirectory, FastZip.Overwrite.Always, null, null, null, true);

            return Result<Empty, Error>.Ok(new Empty());
        }
        catch (Exception e)
        {
            return Result<Empty, Error>.Err(new Error(ErrorKind.DownloadFailure,
                $"an exception '{e.GetType().Name}' has been thrown while downloading configuration: {e.Message}: {e.StackTrace}"));
        }
        finally
        {
            if (File.Exists(packageFile)) File.Delete(packageFile);
        }
    }
}