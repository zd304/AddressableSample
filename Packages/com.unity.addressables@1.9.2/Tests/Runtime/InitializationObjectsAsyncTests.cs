﻿using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.TestTools;

public abstract class InitializationObjectsAsyncTests : AddressablesTestFixture
{
    [UnityTest]
    [Timeout(3000)]
    public IEnumerator InitializationObjects_CompletesWhenNoObjectsPresent()
    {
        InitalizationObjectsOperation op = new InitalizationObjectsOperation();
        op.Completed += obj =>
        {
            Assert.AreEqual(AsyncOperationStatus.Succeeded, obj.Status);
            Assert.IsTrue(obj.Result);
        };
        var runtimeDataLocation = new ResourceLocationBase("RuntimeData", m_RuntimeSettingsPath, typeof(JsonAssetProvider).FullName, typeof(ResourceManagerRuntimeData));
        var rtdOp = m_Addressables.ResourceManager.ProvideResource<ResourceManagerRuntimeData>(runtimeDataLocation);

        op.Init(rtdOp, m_Addressables);

        var handle = m_Addressables.ResourceManager.StartOperation(op, rtdOp);
        yield return handle;
    }

    [Test]
    public void InitializationObjects_OperationRegistersForCallbacks()
    {
        //We're checking to make sure we've created a new ResourceManagerCallbacks object.  If this isn't null
        //then we won't create a new one.  This would never be needed in a legitimate scenario.
        m_Addressables.ResourceManager.m_CallbackHooks = null; 
        int startCount = Resources.FindObjectsOfTypeAll<MonoBehaviourCallbackHooks>().Length;

        InitalizationObjectsOperation op = new InitalizationObjectsOperation();
        var runtimeDataLocation = new ResourceLocationBase("RuntimeData", m_RuntimeSettingsPath, typeof(JsonAssetProvider).FullName, typeof(ResourceManagerRuntimeData));
        var rtdOp = m_Addressables.ResourceManager.ProvideResource<ResourceManagerRuntimeData>(runtimeDataLocation);

        //Test
        op.Init(rtdOp, m_Addressables);
        int endCount = Resources.FindObjectsOfTypeAll<MonoBehaviourCallbackHooks>().Length;

        //Assert
        Assert.AreEqual(startCount + 1, endCount);
    }

#if UNITY_EDITOR
    [UnityTest]
    [Timeout(5000)]
    public IEnumerator InitializationObjects_CompletesWhenObjectsPresent()
    {
        InitalizationObjectsOperation op = new InitalizationObjectsOperation();
        op.Completed += obj =>
        {
            Assert.AreEqual(AsyncOperationStatus.Succeeded, obj.Status);
            Assert.IsTrue(obj.Result);
        };
        var runtimeDataLocation = new ResourceLocationBase("RuntimeData", m_RuntimeSettingsPath, typeof(JsonAssetProvider).FullName, typeof(ResourceManagerRuntimeData));
        var rtdOp = m_Addressables.ResourceManager.ProvideResource<ResourceManagerRuntimeData>(runtimeDataLocation);
        rtdOp.Completed += obj =>
        {
            ObjectInitializationData opData = ObjectInitializationData.CreateSerializedInitializationData<FakeInitializationObject>("fake", "fake");
            obj.Result.InitializationObjects.Add(opData);
        };
        yield return rtdOp;

        op.Init(rtdOp, m_Addressables);

        var handle = m_Addressables.ResourceManager.StartOperation(op, rtdOp);
        yield return handle;
    }
#endif

    [UnityTest]
    [Timeout(3000)]
    public IEnumerator InitializationAsync_HandlesEmptyData()
    {
        InitalizationObjectsOperation op = new InitalizationObjectsOperation();
        op.Completed += obj =>
        {
            Assert.AreEqual(AsyncOperationStatus.Succeeded, obj.Status);
            Assert.IsTrue(obj.Result);
        };
        var runtimeDataLocation = new ResourceLocationBase("RuntimeData", m_RuntimeSettingsPath, typeof(JsonAssetProvider).FullName, typeof(ResourceManagerRuntimeData));
        var rtdOp = m_Addressables.ResourceManager.ProvideResource<ResourceManagerRuntimeData>(runtimeDataLocation);
        rtdOp.Completed += obj =>
        {
            obj.Result.InitializationObjects.Add(default(ObjectInitializationData));
        };
        yield return rtdOp;

        op.Init(rtdOp, m_Addressables);

        var handle = m_Addressables.ResourceManager.StartOperation(op, rtdOp);
        yield return handle;
    }

    [UnityTest]
    public IEnumerator CacheInitializationObject_FullySetsCachingData()
    {
#if ENABLE_CACHING
        //SaveData for cleanup
        CacheInitializationData preTestCacheData = new CacheInitializationData()
        {
            CacheDirectoryOverride = Caching.currentCacheForWriting.path,
            CompressionEnabled = Caching.compressionEnabled,
            ExpirationDelay = Caching.currentCacheForWriting.expirationDelay,
            MaximumCacheSize = Caching.currentCacheForWriting.maximumAvailableStorageSpace
        };

        string cacheDirectoryOverride = "TestDirectory";
        int expirationDelay = 4321;
        long maxCacheSize = 9876;
        bool compressionEnabled = !preTestCacheData.CompressionEnabled;

        CacheInitializationData cacheData = new CacheInitializationData()
        {
            CacheDirectoryOverride = cacheDirectoryOverride,
            CompressionEnabled = compressionEnabled,
            ExpirationDelay = expirationDelay,
            LimitCacheSize = true,
            MaximumCacheSize = maxCacheSize
        };

        string json = JsonUtility.ToJson(cacheData);

        CacheInitialization ci = new CacheInitialization();
        var handle = ci.InitializeAsync(m_Addressables.ResourceManager, "TestCacheInit", json);
        yield return handle;

        Assert.AreEqual(cacheDirectoryOverride, Caching.currentCacheForWriting.path);
        Assert.AreEqual(expirationDelay, Caching.currentCacheForWriting.expirationDelay);
        Assert.AreEqual(compressionEnabled, Caching.compressionEnabled);
        Assert.AreEqual(maxCacheSize, Caching.currentCacheForWriting.maximumAvailableStorageSpace);

        //Cleanup
        Cache cache = Caching.GetCacheByPath(preTestCacheData.CacheDirectoryOverride);
        Caching.compressionEnabled = preTestCacheData.CompressionEnabled;
        cache.maximumAvailableStorageSpace = preTestCacheData.MaximumCacheSize;
        cache.expirationDelay = preTestCacheData.ExpirationDelay;
        Caching.currentCacheForWriting = cache;

        handle.Release();
#else
        yield return null;
        Assert.Ignore();
#endif
    }

    class FakeInitializationObject : IInitializableObject
    {
        internal string m_Id;
        internal string m_Data;

        public bool Initialize(string id, string data)
        {
            m_Id = id;
            m_Data = data;

            return true;
        }

        public AsyncOperationHandle<bool> InitializeAsync(ResourceManager rm, string id, string data)
        {
            FakeAsyncOp op = new FakeAsyncOp();
            return rm.StartOperation(op, default);
        }
    }

    class FakeAsyncOp : AsyncOperationBase<bool>
    {
        protected override void Execute()
        {
            Complete(true, true, "");
        }
    }

#if UNITY_EDITOR
    class InitializationObjects_FastMode : InitializationObjectsAsyncTests { protected override TestBuildScriptMode BuildScriptMode { get { return TestBuildScriptMode.Fast; } } }

    class InitializationObjects_VirtualMode : InitializationObjectsAsyncTests { protected override TestBuildScriptMode BuildScriptMode { get { return TestBuildScriptMode.Virtual; } } }

    class InitializationObjects_PackedPlaymodeMode : InitializationObjectsAsyncTests { protected override TestBuildScriptMode BuildScriptMode { get { return TestBuildScriptMode.PackedPlaymode; } } }
#endif

    [UnityPlatform(exclude = new[] { RuntimePlatform.WindowsEditor, RuntimePlatform.OSXEditor, RuntimePlatform.LinuxEditor })]
    class InitializationObjects_PackedMode : InitializationObjectsAsyncTests { protected override TestBuildScriptMode BuildScriptMode { get { return TestBuildScriptMode.Packed; } } }
}