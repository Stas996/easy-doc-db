﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Mocoding.EasyDocDb.Core;
using Moq;
using Xunit;

namespace Mocoding.EasyDocDb.Tests.Core
{
    public class DocumentTests
    {
        private const string Ref = "test_ref";

        [Fact]
        public void NewDocumentTest()
        {
            var document = new Document<Person>(Ref, null, null);

            Assert.NotNull(document.Data);
        }

        [Fact]
        public async Task SaveDocumentTest()
        {
            var storage = new Mock<IDocumentStorage>();
            var serializer = new Mock<IDocumentSerializer>();

            var document = new Document<Person>(Ref, storage.Object, serializer.Object);
            var expectedContent = "test content";
            serializer.Setup(i => i.Serialize(document.Data)).Returns(expectedContent);

            await document.Save();

            serializer.Object.Serialize(document.Data);
            await storage.Object.Write(Ref, expectedContent);
        }

        [Fact]
        public async Task LoadDocumentTest()
        {
            var storage = new Mock<IDocumentStorage>();
            var serializer = new Mock<IDocumentSerializer>();

            var expectedContent = "test content";
            var expectedName = "test name";
            storage.Setup(i => i.Read(Ref)).Returns(Task.FromResult(expectedContent));
            serializer.Setup(i => i.Deserialize<Person>(expectedContent)).Returns(new Person() { FullName = expectedName });

            var document = new Document<Person>(Ref, storage.Object, serializer.Object);
            await document.Init();

            var doc = document.Data;
            Assert.Equal(expectedName, doc.FullName);
        }

        [Fact]
        public async Task DeleteCallbackTest()
        {
            var storage = new Mock<IDocumentStorage>();
            var serializer = new Mock<IDocumentSerializer>();

            var callbackCalled = false;
            var document = new Document<Person>(Ref, storage.Object, serializer.Object, d =>
            {
                Assert.NotNull(d);
                callbackCalled = true;
            });

            await document.Delete();

            Assert.True(callbackCalled);
            await storage.Object.Delete(Ref);

            callbackCalled = false;

            await document.Delete();

            Assert.False(callbackCalled);
        }

        [Fact]
        public async Task SaveCallbackTest()
        {
            var storage = new Mock<IDocumentStorage>();
            var serializer = new Mock<IDocumentSerializer>();

            var callbackCalled = false;
            var document = new Document<Person>(Ref, storage.Object, serializer.Object, null, d =>
            {
                Assert.NotNull(d);
                callbackCalled = true;
            });
            await document.Save();

            Assert.True(callbackCalled);

            callbackCalled = false;

            await document.Save();

            Assert.False(callbackCalled);
        }

        [Fact]
        public async Task CheckExeption()
        {
            var storage = new Mock<IDocumentStorage>();
            var serializer = new Mock<IDocumentSerializer>();
            var document = new Document<Person>(Ref, storage.Object, serializer.Object);

            var taskUpdate = Task.Run(() => document.SyncUpdate(_ => Thread.Sleep(10000)));

            Exception ex = await Assert.ThrowsAsync<EasyDocDbException>(() => Task.Run(() => document.Save()));

            Assert.Equal("Timeout! Can't get exclusive access to document.", ex.Message);
        }
    }
}
