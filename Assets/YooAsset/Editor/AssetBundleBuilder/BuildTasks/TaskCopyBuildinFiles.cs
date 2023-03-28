using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace YooAsset.Editor
{
	[TaskAttribute("拷贝内置文件到流目录")]
	public class TaskCopyBuildinFiles : IBuildTask
	{
		void IBuildTask.Run(BuildContext context)
		{
			var buildParametersContext = context.GetContextObject<BuildParametersContext>();
			var manifestContext = context.GetContextObject<PackageManifestContext>();
            var buildMapContext = context.GetContextObject<BuildMapContext>();
            var buildMode = buildParametersContext.Parameters.BuildMode;
			if (buildMode == EBuildMode.ForceRebuild || buildMode == EBuildMode.IncrementalBuild)
			{
				if (buildParametersContext.Parameters.CopyBuildinFileOption != ECopyBuildinFileOption.None)
				{
					CopyBuildinFilesToStreaming(buildParametersContext, buildMapContext, manifestContext);
				}
			}
		}

		/// <summary>
		/// 拷贝首包资源文件
		/// </summary>
		private void CopyBuildinFilesToStreaming(BuildParametersContext buildParametersContext, BuildMapContext buildMapContext, PackageManifestContext manifestContext)
		{
            ECopyBuildinFileOption option = buildParametersContext.Parameters.CopyBuildinFileOption;
            string packageOutputDirectory = buildParametersContext.GetPackageOutputDirectory();
            string streamingAssetsDirectory = AssetBundleBuilderHelper.GetStreamingAssetsFolderPath();
            string buildPackageVersion = buildParametersContext.Parameters.PackageVersion;
            // 清空流目录
            if (option == ECopyBuildinFileOption.ClearAndCopyAll || option == ECopyBuildinFileOption.ClearAndCopyByTags)
            {
                AssetBundleBuilderHelper.ClearStreamingAssetsFolder();
            }

            foreach (var item in manifestContext.Manifests)
            {
                // 加载补丁清单
                string buildPackageName = item.Key;
                string packageName = buildPackageName.Split('_')[1];
                PackageManifest packageManifest = item.Value;

                // 拷贝补丁清单文件
                {
                    string sourcePath = $"{packageOutputDirectory}/{packageName}";
                    string destPath = $"{streamingAssetsDirectory}/{packageName}";
                    EditorTools.CopyDirectoryAndChangeBuildInManifest(sourcePath, destPath, "version");
                    EditorTools.CopyDirectoryAndChangeBuildInManifest(sourcePath, destPath, "bytes");
                    EditorTools.CopyDirectoryAndChangeBuildInManifest(sourcePath, destPath, "json");
                }

                // 拷贝文件列表（所有文件）
                if (option == ECopyBuildinFileOption.ClearAndCopyAll || option == ECopyBuildinFileOption.OnlyCopyAll)
                {
                    foreach (var patchBundle in packageManifest.BundleList)
                    {
                        var bundleInfo = buildMapContext.GetBundleInfo(patchBundle.BundleName);
                        //string sourcePath = bundleInfo.PatchInfo.BuildOutputFilePath;
                        string sourcePath = $"{packageOutputDirectory}/{packageName}/{patchBundle.FileName}";
                        string destPath = $"{streamingAssetsDirectory}/{packageName}/{patchBundle.FileName}";
                        EditorTools.CopyFile(sourcePath, destPath, true);
                    }
                }

                // 拷贝文件列表（带标签的文件）
                if (option == ECopyBuildinFileOption.ClearAndCopyByTags || option == ECopyBuildinFileOption.OnlyCopyByTags)
                {
                    string[] tags = buildParametersContext.Parameters.CopyBuildinFileTags.Split(';');
                    foreach (var patchBundle in packageManifest.BundleList)
                    {
                        if (patchBundle.HasTag(tags) == false)
                            continue;
                        string sourcePath = $"{packageOutputDirectory}/{packageName}/{patchBundle.FileName}";
                        var bundleInfo = buildMapContext.GetBundleInfo(patchBundle.BundleName);
                        //string sourcePath = bundleInfo.PatchInfo.BuildOutputFilePath;
                        string destPath = $"{streamingAssetsDirectory}/{packageName}/{patchBundle.FileName}";
                        EditorTools.CopyFile(sourcePath, destPath, true);
                    }
                }

            }

            // 刷新目录
            AssetDatabase.Refresh();
            BuildLogger.Log($"内置文件拷贝完成:");//($"内置文件拷贝完成：{streamingAssetsDirectory}");
		}
	}
}