namespace SimpleObjectStorage.Tests;

public class SimpleObjectStorageKeyTests
{
    [Test]
    public void TestValidKeys()
    {
        var keys = new string [] {
            "a.txt",
            "a.bin",
            "sub/b.txt",
            "a/b.txt",
            "a/b/c.txt",
            "0.txt",
            "0/1.txt",
            "0.0",
            "a-b.txt",
            // 128 character file name
            $"{new string('a', 128)}.txt",
            // 128 character directory name
            $"{new string('a', 128)}/a.txt",
            $"{new string('a', 128)}/{new string('a', 128)}.txt",
            // 128 character directory name with 128 character file name
            $"{new string('a', 128)}/{new string('a', 128)}/{new string('a', 128)}.txt",
            // 128 character file extension
            $"a.{new string('a', 128)}",
            // 128 character directory name with 128 character file extension
            $"{new string('a', 128)}/{new string('a', 128)}/{new string('a', 128)}.{new string('a', 128)}",
        };

        foreach (var key in keys)
        {
            Assert.IsTrue(IObjectStore.IsValidKey(key));
        }
    }

    [Test]
    public void TestInvalidKeys()
    {
        var keys = new string []{
            "",
            "-",
            "a-",
            ".",
            "-.-",
            "a-.",
        };

        foreach (var key in keys)
        {
            Assert.IsFalse(IObjectStore.IsValidKey(key));
        }
    }
}

public class LocalStorageInstanceTests
{
    [Test]
    public void TestInstanceCreationWithValidPath()
    {
        var tempPath = Path.GetTempPath();
        var storage = new LocalFileObjectStore(tempPath);
        Assert.IsNotNull(storage);
    }

    [Test]
    public void TestInstanceCreationWithInvalidPath()
    {
        var tempPath = Path.GetTempPath();
        var nonExistentPath = Path.Combine(tempPath, $"{Guid.NewGuid()}");
        
        // make sure instance creation throws some exception
        Assert.Throws<Exception>(() => new LocalFileObjectStore(nonExistentPath));
    }
}

public class LocalStorageSetTests
{
    private string _folderPath = string.Empty;

    [SetUp]
    public void Setup()
    {
        _folderPath = Path.GetTempPath();
    }

    [Test]
    public async Task TestSetTestData()
    {
        var storage = new LocalFileObjectStore(_folderPath);
        var key = "a.txt";
        var returnCode = await storage.SetTextFileAsync(key, "Hello World");
        Assert.That(returnCode, Is.EqualTo(ObjectStoreReturnCodes.Success));
    }

    [Test]
    public async Task TestSetBinaryData()
    {
        var storage = new LocalFileObjectStore(_folderPath);
        var key = "a.bin";
        var returnCode = await storage.SetBinaryFileAsync(key, new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });
        Assert.That(returnCode, Is.EqualTo(ObjectStoreReturnCodes.Success));

    }
}

public class LocalStorageGetTests
{
    private LocalFileObjectStore? _storage;

    [SetUp]
    public async Task Setup()
    {
        var tempPath = Path.GetTempPath();
        _storage = new LocalFileObjectStore(tempPath);
        await _storage.SetTextFileAsync("a.txt", "Hello World");
        Directory.CreateDirectory(Path.Combine(tempPath, "sub"));
        await _storage.SetTextFileAsync("sub/b.txt", "Hello World");
    }

    [Test]
    public async Task GetFileThatDoesNotExist()
    {
        var key = "c.txt";
        var returnCode = await _storage!.GetTextFileAsync(key);
        Assert.That(returnCode.ReturnCode, Is.EqualTo(ObjectStoreReturnCodes.FileNotFound));
    }

    [Test]
    public async Task GetFileThatExists()
    {
        var key = "a.txt";
        var returnCode = await _storage!.GetTextFileAsync(key);
        Assert.That(returnCode.ReturnCode, Is.EqualTo(ObjectStoreReturnCodes.Success));
    }

    [Test]
    public async Task GetFileThatExistsWithSubDirectory()
    {
        var key = "sub/b.txt";
        var returnCode = await _storage!.GetTextFileAsync(key);
        Assert.That(returnCode.ReturnCode, Is.EqualTo(ObjectStoreReturnCodes.Success));
    }
}

public class LocalStorageDeleteTests
{
    private LocalFileObjectStore? _storage;

    [SetUp]
    public async Task Setup()
    {
        _storage = new LocalFileObjectStore(Path.GetTempPath());
        await _storage.SetTextFileAsync("a.txt", "Hello World");
    }

    [Test]
    public async Task DeleteFileThatDoesNotExist()
    {
        var key = "c.txt";
        var returnCode = await _storage!.DeleteAsync(key);
        Assert.That(returnCode, Is.EqualTo(ObjectStoreReturnCodes.Success));
    }

    [Test]
    public async Task DeleteFileThatExists()
    {
        var key = "a.txt";
        var returnCode = await _storage!.DeleteAsync(key);
        Assert.That(returnCode, Is.EqualTo(ObjectStoreReturnCodes.Success));
        var response = _storage.GetTextFileAsync(key);
        Assert.That(response.Result.ReturnCode, Is.EqualTo(ObjectStoreReturnCodes.FileNotFound));
    }
}