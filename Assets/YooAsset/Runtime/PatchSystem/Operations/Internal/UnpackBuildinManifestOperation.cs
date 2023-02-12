
namespace YooAsset
{
	internal class UnpackBuildinManifestOperation : AsyncOperationBase
	{
		private enum ESteps
		{
			None,
			UnpackManifestHashFile,
			UnpackManifestFile,
			Done,
		}

		private readonly string _buildinPackageName;
		private readonly string _buildinPackageVersion;
		private UnityWebFileRequester _downloader1;
		private UnityWebFileRequester _downloader2;
		private ESteps _steps = ESteps.None;

		public UnpackBuildinManifestOperation(string buildinPackageName)
		{
			_buildinPackageName = buildinPackageName;
		}
		internal override void Start()
		{
			_steps = ESteps.UnpackManifestHashFile;
		}
		internal override void Update()
		{
			if (_steps == ESteps.None || _steps == ESteps.Done)
				return;

			if(_steps == ESteps.UnpackManifestHashFile)
			{
				if (_downloader1 == null)
				{
					string savePath = PersistentHelper.GetCachePackageVersionFilePath(_buildinPackageName);
					string fileName = YooAssetSettingsData.GetPackageVersionFileName(_buildinPackageName);
					string filePath = PathHelper.MakeStreamingLoadPath(fileName);
					string url = PathHelper.ConvertToWWWPath(filePath);
					_downloader1 = new UnityWebFileRequester();
					_downloader1.SendRequest(url, savePath);
				}

				if (_downloader1.IsDone() == false)
					return;

				if (_downloader1.HasError())
				{
					_steps = ESteps.Done;
					Status = EOperationStatus.Failed;
					Error = _downloader1.GetError();
				}
				else
				{
					_steps = ESteps.UnpackManifestFile;
				}

				_downloader1.Dispose();
			}

			if (_steps == ESteps.UnpackManifestFile)
			{
				if (_downloader2 == null)
				{
					string savePath = PersistentHelper.GetCacheManifestFilePath(_buildinPackageName);
					string fileName = YooAssetSettingsData.GetManifestBinaryFileName(_buildinPackageName);
					string filePath = PathHelper.MakeStreamingLoadPath(fileName);
					string url = PathHelper.ConvertToWWWPath(filePath);
					_downloader2 = new UnityWebFileRequester();
					_downloader2.SendRequest(url, savePath);
				}

				if (_downloader2.IsDone() == false)
					return;

				if (_downloader2.HasError())
				{
					_steps = ESteps.Done;
					Status = EOperationStatus.Failed;
					Error = _downloader2.GetError();
				}
				else
				{
					_steps = ESteps.Done;
					Status = EOperationStatus.Succeed;
				}

				_downloader2.Dispose();
			}
		}
	}
}