namespace SimpleObjectStorage.Tests;

public class SimpleObjectStorageKeyTests
{
    [SetUp]
    public void Setup()
    {
        
    }

    [Test]
    public void TestValidKeys()
    {
        // Valid matches for @"^([a-zA-Z0-9][a-zA-Z0-9-]{0,127}/)*([a-zA-Z0-9][a-zA-Z0-9-]{0,127}\.[a-zA-Z0-9]{1,128})$";
        var keys = new string [] {
            "a.txt",
            "a.bin",
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

public class SimpleObjectStorageInstanceTests
{
    [SetUp]
    public void Setup()
    {
        
    }

    [Test]
    public void Test()
    {
        Assert.Pass();
    }
}

public class SimpleObjectStorageSetTests
{
    [SetUp]
    public void Setup()
    {
        
    }

    [Test]
    public void Test()
    {
        Assert.Pass();
    }
}

public class SimpleObjectStorageGetTests
{
    [SetUp]
    public void Setup()
    {
        
    }

    [Test]
    public void Test()
    {
        Assert.Pass();
    }
}

public class SimpleObjectStorageDeleteTests
{
    [SetUp]
    public void Setup()
    {
        
    }

    [Test]
    public void Test()
    {
        Assert.Pass();
    }
}