using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UniFramework.Event;
using UniFramework.Module;
using UnityEngine;
using YooAsset;

public class StartUp : MonoBehaviour
{
    public EPlayMode playMode = EPlayMode.HostPlayMode;
	public string baseUrl = "http://127.0.0.1:8080/";
	public string gameVersion;
	public string packageVersion;

	private void Start()
    {
        InitApp();

        //LoadingSceneManager.Instance.LoadSceneAsync(GlobalConfigs.UpdateScene, false);
    }

    public void InitApp()
    {
		// ��ʼ��BetterStreaming
		BetterStreamingAssets.Initialize();

		// ��ʼ���¼�ϵͳ
		UniEvent.Initalize();

		// ��ʼ������ϵͳ
		UniModule.Initialize();

		// ��ʼ����Դϵͳ
		YooAssets.Initialize();
		YooAssets.SetOperationSystemMaxTimeSlice(30);

		UniModule.StartCoroutine(InitPackage());

    }

	private IEnumerator InitPackage()
	{
		// ����Ĭ�ϵ���Դ��
		string packageName = "DefaultPackage";
		var package = YooAssets.TryGetAssetsPackage(packageName);
		if (package == null)
		{
			package = YooAssets.CreateAssetsPackage(packageName, packageVersion);
			YooAssets.SetDefaultAssetsPackage(package);
		}

		// �༭���µ�ģ��ģʽ
		InitializationOperation initializationOperation = null;
		if (playMode == EPlayMode.EditorSimulateMode)
		{
			var createParameters = new EditorSimulateModeParameters();
			createParameters.SimulatePatchManifestPath = EditorSimulateModeHelper.SimulateBuild(packageName);
			initializationOperation = package.InitializeAsync(createParameters);
		}

		// ��������ģʽ
		if (playMode == EPlayMode.OfflinePlayMode)
		{
			var createParameters = new OfflinePlayModeParameters();
			createParameters.DecryptionServices = new GameDecryptionServices();
			initializationOperation = package.InitializeAsync(createParameters);
		}

		// ��������ģʽ
		if (playMode == EPlayMode.HostPlayMode)
		{
			var createParameters = new HostPlayModeParameters();
			createParameters.DecryptionServices = new GameDecryptionServices();
			createParameters.QueryServices = new GameQueryServices();
			createParameters.DefaultHostServer = GetHostServerURL();
			createParameters.FallbackHostServer = GetHostServerURL();
			initializationOperation = package.InitializeAsync(createParameters);
		}

		yield return initializationOperation;
		if (package.InitializeStatus == EOperationStatus.Succeed)
		{
			UniModule.StartCoroutine(GetStaticVersion(package));
		}
		else
		{
			Debug.LogWarning($"{initializationOperation.Error}");
			PatchEventDefine.InitializeFailed.SendEventMessage();
		}
	}

	/// <summary>
	/// ��ȡ��Դ��������ַ
	/// </summary>
	private string GetHostServerURL()
	{
#if UNITY_EDITOR
		if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.Android)
			return $"{baseUrl}/Android/{gameVersion}";
		else if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.iOS)
			return $"{baseUrl}/iOS/{gameVersion}";
		else if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.WebGL)
			return $"{baseUrl}/WebGL/{gameVersion}";
		else
			return $"{baseUrl}/StandaloneWindows64/{gameVersion}";
#else
		return "";
#endif
	}

	private IEnumerator GetStaticVersion(AssetsPackage package)
	{
		var operation = package.UpdatePackageVersionAsync();
		yield return operation;

		if (operation.Status == EOperationStatus.Succeed)
		{
			UniModule.StartCoroutine(UpdateManifest(package, operation.PackageVersion.crc));
		}
		else
		{
			Debug.LogWarning(operation.Error);
			PatchEventDefine.PackageVersionUpdateFailed.SendEventMessage();
		}
	}

	private IEnumerator UpdateManifest(AssetsPackage package,string packageVersion)
	{
		var operation = package.UpdatePackageManifestAsync(packageVersion);
		yield return operation;

		if (operation.Status == EOperationStatus.Succeed)
		{
			UniModule.StartCoroutine(CreateDownloader());
		}
		else
		{
			Debug.LogWarning(operation.Error);
			PatchEventDefine.PatchManifestUpdateFailed.SendEventMessage();
		}
	}

	IEnumerator CreateDownloader()
	{
		yield return new WaitForSecondsRealtime(0.5f);

		int downloadingMaxNum = 10;
		int failedTryAgain = 3;
		var downloader = YooAssets.CreatePatchDownloader(downloadingMaxNum, failedTryAgain);

		if (downloader.TotalDownloadCount == 0)
		{
			Debug.Log("Not found any download files !");
			PatchEventDefine.PatchStatesChange.SendEventMessage("����δʹ�õĻ����ļ���");
			var package = YooAsset.YooAssets.GetAssetsPackage("DefaultPackage");
			var operation = package.ClearUnusedCacheFilesAsync();
			operation.Completed += Operation_Completed;
		}
		else
		{
			//A total of 10 files were found that need to be downloaded
			Debug.Log($"Found total {downloader.TotalDownloadCount} files that need download ��");

			// �����¸����ļ��󣬹�������ϵͳ
			// ע�⣺��������Ҫ������ǰ�����̿ռ䲻��
			int totalDownloadCount = downloader.TotalDownloadCount;
			long totalDownloadBytes = downloader.TotalDownloadBytes;
			PatchEventDefine.FoundUpdateFiles.SendEventMessage(totalDownloadCount, totalDownloadBytes);
			UniModule.StartCoroutine(BeginDownload(downloader));
		}
	}

	private IEnumerator BeginDownload(PatchDownloaderOperation downloader)
	{
		// ע�����ػص�
		downloader.OnDownloadErrorCallback = PatchEventDefine.WebFileDownloadFailed.SendEventMessage;
		downloader.OnDownloadProgressCallback = PatchEventDefine.DownloadProgressUpdate.SendEventMessage;
		downloader.BeginDownload();
		yield return downloader;

		// ������ؽ��
		if (downloader.Status != EOperationStatus.Succeed)
			yield break;

		Operation_Completed(null);
	}

	private void Operation_Completed(YooAsset.AsyncOperationBase obj)
	{
		PatchEventDefine.PatchStatesChange.SendEventMessage("��ʼ��Ϸ��");

		// ������Ϸ������
		UniModule.CreateModule<GameManager>();

		// ������Ϸ����
		GameManager.Instance.Run();
	}

	/// <summary>
	/// �����ļ���ѯ������
	/// </summary>
	private class GameQueryServices : IQueryServices
	{
		public bool QueryStreamingAssets(string packageName, string fileName)
		{
			// ע�⣺ʹ����BetterStreamingAssets�����ʹ��ǰ��Ҫ��ʼ���ò����
			string buildinFolderName = YooAssets.GetStreamingAssetBuildinFolderName();
			var exist = BetterStreamingAssets.FileExists($"{buildinFolderName}/{packageName}/{fileName}");
			Debug.Log($"{buildinFolderName}/{packageName}/{fileName} exist : {exist}");
			return exist;
		}
	}

	/// <summary>
	/// ��Դ�ļ����ܷ�����
	/// </summary>
	private class GameDecryptionServices : IDecryptionServices
	{
		public ulong LoadFromFileOffset(DecryptFileInfo fileInfo)
		{
			return 32;
		}

		public byte[] LoadFromMemory(DecryptFileInfo fileInfo)
		{
			throw new NotImplementedException();
		}

		public FileStream LoadFromStream(DecryptFileInfo fileInfo)
		{
			BundleStream bundleStream = new BundleStream(fileInfo.FilePath, FileMode.Open);
			return bundleStream;
		}

		public uint GetManagedReadBufferSize()
		{
			return 1024;
		}
	}

}
