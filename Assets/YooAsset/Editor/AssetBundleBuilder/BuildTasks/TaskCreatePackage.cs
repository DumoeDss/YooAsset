using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace YooAsset.Editor
{
	[TaskAttribute("制作包裹")]
	public class TaskCreatePackage : IBuildTask
	{
		void IBuildTask.Run(BuildContext context)
		{
			var buildParameters = context.GetContextObject<BuildParametersContext>();
			var buildMapContext = context.GetContextObject<BuildMapContext>();
			var patchManifestContext = context.GetContextObject<ManifestContext>();
			
			var buildMode = buildParameters.Parameters.BuildMode;
			if (buildMode == EBuildMode.ForceRebuild || buildMode == EBuildMode.IncrementalBuild)
			{
				CopyPackageFiles(buildParameters, buildMapContext, patchManifestContext);
			}
		}

		/// <summary>
		/// 拷贝补丁文件到补丁包目录
		/// </summary>
		private void CopyPackageFiles(BuildParametersContext buildParametersContext, BuildMapContext buildMapContext, ManifestContext patchManifestContext)
		{
			var buildParameters = buildParametersContext.Parameters;
			string pipelineOutputDirectory = buildParametersContext.GetPipelineOutputDirectory();
			string packageOutputDirectory = buildParametersContext.GetPackageOutputDirectory();
			BuildLogger.Log($"开始拷贝补丁文件到补丁包目录：{packageOutputDirectory}");

			foreach (var PackageManifest in patchManifestContext.Manifests)
			{
				// 拷贝所有补丁文件
				int progressValue = 0;
				PackageManifest manifest = PackageManifest.Value;
				int packageFileTotalCount = manifest.BundleList.Count;
				foreach (var packageBundle in manifest.BundleList)
				{
					var bundleInfo = buildMapContext.GetBundleInfo(packageBundle.BundleName);
					string sourcePath = bundleInfo.BundleInfo.BuildOutputFilePath;
					string destPath = bundleInfo.BundleInfo.PackageOutputFilePath;
					EditorTools.CopyFile(sourcePath, destPath, true);

					EditorTools.DisplayProgressBar("拷贝补丁文件", ++progressValue, packageFileTotalCount);
				}
			}

			if (buildParameters.BuildPipeline == EBuildPipeline.ScriptableBuildPipeline)
			{
				// 拷贝构建日志
				{
					string sourcePath = $"{pipelineOutputDirectory}/buildlogtep.json";
					string destPath = $"{packageOutputDirectory}/buildlogtep.json";
					EditorTools.CopyFile(sourcePath, destPath, true);
				}

				// 拷贝代码防裁剪配置
				if (buildParameters.SBPParameters.WriteLinkXML)
				{
					string sourcePath = $"{pipelineOutputDirectory}/link.xml";
					string destPath = $"{packageOutputDirectory}/link.xml";
					EditorTools.CopyFile(sourcePath, destPath, true);
				}
			}
			else if (buildParameters.BuildPipeline == EBuildPipeline.BuiltinBuildPipeline)
			{
				// 拷贝UnityManifest序列化文件
				{
					string sourcePath = $"{pipelineOutputDirectory}";
					string destPath = $"{packageOutputDirectory}/{YooAssetSettings.OutputFolderName}";
					EditorTools.CopyDirectory(sourcePath, destPath);
					EditorTools.DeleteDirectory(sourcePath);
				}
			}
			else
			{
				throw new System.NotImplementedException();
			}

			EditorTools.ClearProgressBar();
		}
	}
}