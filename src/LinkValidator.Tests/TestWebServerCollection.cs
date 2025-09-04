// -----------------------------------------------------------------------
// <copyright file="TestWebServerCollection.cs">
//      Copyright (C) 2025 - 2025 Aaron Stannard <https://aaronstannard.com/>
// </copyright>
// -----------------------------------------------------------------------

namespace LinkValidator.Tests;

[CollectionDefinition("WebServer")]
public class TestWebServerCollection : ICollectionFixture<TestWebServerFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}