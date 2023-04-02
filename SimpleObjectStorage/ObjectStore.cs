using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Amazon.S3;
using Amazon.S3.Model;

namespace SimpleObjectStorage;

public enum ObjectStoreReturnCodes
{
    Success = 0,
    InvalidKey = 1,
    FileNotFound = 2,
    FileAlreadyExists = 3,
    UnknownError = 4
}

public record ObjectStoreTextResult(ObjectStoreReturnCodes ReturnCode, string Value)
{
    public static ObjectStoreTextResult FromReturnCode(ObjectStoreReturnCodes returnCode) => new(returnCode, string.Empty);
    public static async Task<ObjectStoreTextResult> FromFile(string filePath)
    {
        try
        {
            var value = await File.ReadAllTextAsync(filePath);
            return Success(value);
        }
        catch
        {
            return new ObjectStoreTextResult(ObjectStoreReturnCodes.UnknownError, string.Empty);
        }
    }
    public static async Task<ObjectStoreTextResult> FromStream(Stream stream)
    {
        try
        {
            using var streamReader = new StreamReader(stream);
            var value = await streamReader.ReadToEndAsync();
            return Success(value);
        }
        catch
        {
            return new ObjectStoreTextResult(ObjectStoreReturnCodes.UnknownError, string.Empty);
        }    
    }
    public static ObjectStoreTextResult Success(string value) => new(ObjectStoreReturnCodes.Success, value);
    public static ObjectStoreTextResult FileNotFound() => new(ObjectStoreReturnCodes.FileNotFound, string.Empty);
    public static ObjectStoreTextResult InvalidKey() => new(ObjectStoreReturnCodes.InvalidKey, string.Empty);
    public static ObjectStoreTextResult FileAlreadyExists() => new(ObjectStoreReturnCodes.FileAlreadyExists, string.Empty);
    public static ObjectStoreTextResult UnknownError() => new(ObjectStoreReturnCodes.UnknownError, string.Empty);
    public bool IsSuccess => ReturnCode == ObjectStoreReturnCodes.Success;
}

public record ObjectStoreBinaryResult(ObjectStoreReturnCodes ReturnCode, byte[] Value)
{
    public static ObjectStoreBinaryResult FromReturnCode(ObjectStoreReturnCodes returnCode) => new(returnCode, Array.Empty<byte>());
    public static async Task<ObjectStoreBinaryResult> FromFileAsync(string filePath)
    {
        try
        {
            var value = await File.ReadAllBytesAsync(filePath) ?? Array.Empty<byte>();
            return Success(value);
        }
        catch
        {
            return new ObjectStoreBinaryResult(ObjectStoreReturnCodes.UnknownError, Array.Empty<byte>());
        }
    }
    public static async Task<ObjectStoreBinaryResult> FromStream(Stream stream)
    {
        try
        {
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            var value = memoryStream.ToArray();
            return Success(value);
        }
        catch
        {
            return new ObjectStoreBinaryResult(ObjectStoreReturnCodes.UnknownError, Array.Empty<byte>());
        }
    }
    public static ObjectStoreBinaryResult Success(byte[] value) => new(ObjectStoreReturnCodes.Success, value);
    public static ObjectStoreBinaryResult FileNotFound() => new(ObjectStoreReturnCodes.FileNotFound, Array.Empty<byte>());
    public static ObjectStoreBinaryResult InvalidKey() => new(ObjectStoreReturnCodes.InvalidKey, Array.Empty<byte>());
    public static ObjectStoreBinaryResult FileAlreadyExists() => new(ObjectStoreReturnCodes.FileAlreadyExists, Array.Empty<byte>());
    public static ObjectStoreBinaryResult UnknownError() => new(ObjectStoreReturnCodes.UnknownError, Array.Empty<byte>());
    public bool IsSuccess => ReturnCode == ObjectStoreReturnCodes.Success;
}

public interface IObjectStore
{
    // KEY := FOLDER | FILE
    // FOLDER := FOLDER_NAME "/" FOLDER | FOLDER_NAME
    // FOLDER_NAME := [a-zA-Z][a-zA-Z0-9-]{0,15}
    // FILE := FILE_NAME "." FILE_EXTENSION
    // file name starts with a letter or a number and can contain only letters, numbers, and hyphens and must be between 1 and 128 characters long
    // FILE_NAME := [a-zA-Z0-9][a-zA-Z0-9-]{0,127}
    // FILE_EXTENSION := [a-zA-Z0-9]{1,128}
    private const string KeyRegex = @"^([a-zA-Z0-9][a-zA-Z0-9-]{0,127}/)*([a-zA-Z0-9][a-zA-Z0-9-]{0,127}\.[a-zA-Z0-9]{1,128})$";
    public static bool IsValidKey(string key) => Regex.IsMatch(key, KeyRegex);

    public Task<ObjectStoreTextResult> GetTextFileAsync(string key);
    public Task <ObjectStoreReturnCodes> SetTextFileAsync(string key, string value);
    public Task<ObjectStoreBinaryResult> GetBinaryFileAsync(string key);
    public Task<ObjectStoreReturnCodes> SetBinaryFileAsync(string key, byte[] value);
    public Task<ObjectStoreReturnCodes> DeleteAsync(string key);
}

public class LocalFileObjectStore : IObjectStore
{
    private readonly string _folderPath;

    public LocalFileObjectStore(string folderPath)
    {
        _folderPath = folderPath;

        if (Directory.Exists(_folderPath))
            return;
        
        
        throw new Exception($"Folder {_folderPath} does not exist.");
    }

    public async Task<ObjectStoreTextResult> GetTextFileAsync(string key)
    {
        var returnCode = FileCheck(key);
        if (returnCode != ObjectStoreReturnCodes.FileAlreadyExists) 
            return ObjectStoreTextResult.FromReturnCode(ObjectStoreReturnCodes.FileNotFound);

        var filePath = Path.Combine(_folderPath, key);
        return await ObjectStoreTextResult.FromFile(filePath);
    }

    public async Task<ObjectStoreReturnCodes> SetTextFileAsync(string key, string value)
    {
        var returnCode = FileCheck(key);
        if (returnCode != ObjectStoreReturnCodes.Success && returnCode != ObjectStoreReturnCodes.FileAlreadyExists)
            return returnCode;

        var filePath = Path.Combine(_folderPath, key);
        try
        {
            await File.WriteAllTextAsync(filePath, value);
            return ObjectStoreReturnCodes.Success;
        }
        catch (Exception)
        {
            return ObjectStoreReturnCodes.UnknownError;
        }
    }

    public async Task<ObjectStoreBinaryResult> GetBinaryFileAsync(string key)
    {
        var returnCode = FileCheck(key);
        if (returnCode != ObjectStoreReturnCodes.Success)
            return ObjectStoreBinaryResult.FromReturnCode(returnCode);

        var filePath = Path.Combine(_folderPath, key);
        return await ObjectStoreBinaryResult.FromFileAsync(filePath);
    }

    public async Task<ObjectStoreReturnCodes> SetBinaryFileAsync(string key, byte[] value)
    {
        var returnCode = FileCheck(key);
        if (returnCode != ObjectStoreReturnCodes.Success && returnCode != ObjectStoreReturnCodes.FileAlreadyExists)
            return returnCode;

        var filePath = Path.Combine(_folderPath, key);
        try
        {
            await File.WriteAllBytesAsync(filePath, value);
            return ObjectStoreReturnCodes.Success;
        }
        catch (Exception)
        {
            return ObjectStoreReturnCodes.UnknownError;
        }
    }

    public async Task<ObjectStoreReturnCodes> DeleteAsync(string key)
    {
        var returnCode = FileCheck(key);
        if (returnCode != ObjectStoreReturnCodes.FileAlreadyExists)
            return returnCode;

        
        var filePath = Path.Combine(_folderPath, key);
        try
        {
            await Task.Run(() => File.Delete(filePath));
            return ObjectStoreReturnCodes.Success;
        }
        catch (Exception)
        {
            return ObjectStoreReturnCodes.UnknownError;
        }
    }

    private ObjectStoreReturnCodes FileCheck(string key)
    {
        if (!IObjectStore.IsValidKey(key))
            return ObjectStoreReturnCodes.InvalidKey;
        var filePath = Path.Combine(_folderPath, key);
        if (File.Exists(filePath))
            return ObjectStoreReturnCodes.FileAlreadyExists;
        return ObjectStoreReturnCodes.Success;
    }
}

public class DigitalOceanSpacesObjectStore : IObjectStore
{
    private readonly AmazonS3Client _client;
    private readonly string _bucketName;

    public DigitalOceanSpacesObjectStore(string accessKey, string secretKey, string spaceName, string region)
    {
        var client = new AmazonS3Client(accessKey, secretKey, new AmazonS3Config
        {
            ServiceURL = $"https://{region}.digitaloceanspaces.com",
            ForcePathStyle = true
        });
        _client = client;
        _bucketName = spaceName;
    }

    public async Task<ObjectStoreTextResult> GetTextFileAsync(string key)
    {
        var returnCode = KeyCheck(key);
        if (returnCode != ObjectStoreReturnCodes.Success)
            return ObjectStoreTextResult.FromReturnCode(returnCode);

        try
        {
            var response = await _client.GetObjectAsync(_bucketName, key);
            using var reader = new StreamReader(response.ResponseStream);
            var content = await reader.ReadToEndAsync();
            return ObjectStoreTextResult.Success(content);
        }
        catch (AmazonS3Exception e)
        {
            if (e.StatusCode == HttpStatusCode.NotFound)
                return ObjectStoreTextResult.FileNotFound();
            return ObjectStoreTextResult.UnknownError();
        }
        catch (Exception)
        {
            return ObjectStoreTextResult.UnknownError();
        }
    }

    public async Task<ObjectStoreReturnCodes> SetTextFileAsync(string key, string value)
    {
        var returnCode = KeyCheck(key);
        if (returnCode != ObjectStoreReturnCodes.Success)
            return returnCode;

        try
        {
            await _client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = key,
                ContentBody = value
            });
            return ObjectStoreReturnCodes.Success;
        }
        catch (Exception)
        {
            return ObjectStoreReturnCodes.UnknownError;
        }
    }

    public async Task<ObjectStoreBinaryResult> GetBinaryFileAsync(string key)
    {
        var returnCode = KeyCheck(key);
        if (returnCode != ObjectStoreReturnCodes.Success)
            return ObjectStoreBinaryResult.FromReturnCode(returnCode);

        try
        {
            var response = await _client.GetObjectAsync(_bucketName, key);
            using var reader = new StreamReader(response.ResponseStream);
            var content = await reader.ReadToEndAsync();
            return ObjectStoreBinaryResult.Success(Encoding.UTF8.GetBytes(content));
        }
        catch (AmazonS3Exception e)
        {
            if (e.StatusCode == HttpStatusCode.NotFound)
                return ObjectStoreBinaryResult.FileNotFound();
            return ObjectStoreBinaryResult.UnknownError();
        }
        catch (Exception)
        {
            return ObjectStoreBinaryResult.UnknownError();
        }
    }

    public async Task<ObjectStoreReturnCodes> SetBinaryFileAsync(string key, byte[] value)
    {
        var returnCode = KeyCheck(key);
        if (returnCode != ObjectStoreReturnCodes.Success)
            return returnCode;

        try
        {
            await _client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = key,
                ContentBody = Encoding.UTF8.GetString(value)
            });
            return ObjectStoreReturnCodes.Success;
        }
        catch (Exception)
        {
            return ObjectStoreReturnCodes.UnknownError;
        }
    }

    public async Task<ObjectStoreReturnCodes> DeleteAsync(string key)
    {
        var returnCode = KeyCheck(key);
        if (returnCode != ObjectStoreReturnCodes.Success)
            return returnCode;

        try
        {
            await _client.DeleteObjectAsync(_bucketName, key);
            return ObjectStoreReturnCodes.Success;
        }
        catch (AmazonS3Exception e)
        {
            if (e.StatusCode == HttpStatusCode.NotFound)
                return ObjectStoreReturnCodes.FileNotFound;
            return ObjectStoreReturnCodes.UnknownError;
        }
        catch (Exception)
        {
            return ObjectStoreReturnCodes.UnknownError;
        }
    }

    private ObjectStoreReturnCodes KeyCheck(string key) => IObjectStore.IsValidKey(key) ? ObjectStoreReturnCodes.Success : ObjectStoreReturnCodes.InvalidKey;
}