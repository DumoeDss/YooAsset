using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Interfaces;

namespace YooAsset.Editor
{
	public class PatchManifestContext : IContextObject
	{
		internal PatchManifest Manifest;
	}

	[TaskAttribute("创建补丁清单文件")]
	public class TaskCreatePatchManifest : IBuildTask
	{
		void IBuildTask.Run(BuildContext context)
		{
			CreatePatchManifestFile(context);
		}

		/// <summary>
		/// 创建补丁清单文件到输出目录
		/// </summary>
		private void CreatePatchManifestFile(BuildContext context)
		{
			var buildMapContext = context.GetContextObject<BuildMapContext>();
			var buildParametersContext = context.GetContextObject<BuildParametersContext>();
			var buildParameters = buildParametersContext.Parameters;
			string packageOutputDirectory = buildParametersContext.GetPackageOutputDirectory();

			// 创建新补丁清单
			PatchManifest patchManifest = new PatchManifest();
			patchManifest.FileVersion = YooAssetSettings.PatchManifestFileVersion;
			patchManifest.EnableAddressable = buildMapContext.EnableAddressable;
			patchManifest.OutputNameStyle = (int)buildParameters.OutputNameStyle;
			patchManifest.PackageName = buildParameters.PackageName;
			patchManifest.PackageVersion = buildParameters.PackageVersion;
			patchManifest.BundleList = GetAllPatchBundle(context);
			List<string> bundleNameList;
			List<string> dependBundleNameList;
			patchManifest.AssetList = GetAllPatchAsset(context, patchManifest, patchManifest.PackageName,out bundleNameList, out dependBundleNameList);
			patchManifest.BundleNameList = bundleNameList.ToArray();
			patchManifest.DependBundleNameList = dependBundleNameList.ToArray();
			// 更新Unity内置资源包的引用关系
			string shadersBunldeName = YooAssetSettingsData.GetUnityShadersBundleFullName(buildMapContext.UniqueBundleName, buildParameters.PackageName);
			if (buildParameters.BuildPipeline == EBuildPipeline.ScriptableBuildPipeline)
			{
				if (buildParameters.BuildMode == EBuildMode.IncrementalBuild)
				{
					var buildResultContext = context.GetContextObject<TaskBuilding_SBP.BuildResultContext>();
					UpdateBuiltInBundleReference(patchManifest, buildResultContext.Results, shadersBunldeName);
				}
			}

			// 创建补丁清单文本文件
			{
				string fileName = YooAssetSettingsData.GetManifestJsonFileName(buildParameters.PackageName, buildParameters.PackageVersion);
				string filePath = $"{packageOutputDirectory}/{fileName}";
				PatchManifestTools.SerializeToJson(filePath, patchManifest);
				BuildRunner.Log($"创建补丁清单文件：{filePath}");
			}

			// 创建补丁清单二进制文件
			string packageHash;
			{
				string fileName = YooAssetSettingsData.GetManifestBinaryFileName(buildParameters.PackageName, buildParameters.PackageVersion);
				string filePath = $"{packageOutputDirectory}/{fileName}";
				PatchManifestTools.SerializeToBinary(filePath, patchManifest);
				packageHash = HashUtility.FileMD5(filePath);
				BuildRunner.Log($"创建补丁清单文件：{filePath}");

				PatchManifestContext patchManifestContext = new PatchManifestContext();
				byte[] bytesData = FileUtility.ReadAllBytes(filePath);
				patchManifestContext.Manifest = PatchManifestTools.DeserializeFromBinary(bytesData);
				context.SetContextObject(patchManifestContext);
			}

			// 创建补丁清单哈希文件
			{
				string fileName = YooAssetSettingsData.GetPackageHashFileName(buildParameters.PackageName, buildParameters.PackageVersion);
				string filePath = $"{packageOutputDirectory}/{fileName}";
				FileUtility.CreateFile(filePath, packageHash);
				BuildRunner.Log($"创建补丁清单哈希文件：{filePath}");
			}

			// 创建补丁清单版本文件
			{
				string fileName = YooAssetSettingsData.GetPackageVersionFileName(buildParameters.PackageName);
				string filePath = $"{packageOutputDirectory}/{fileName}";
				FileUtility.CreateFile(filePath, buildParameters.PackageVersion);
				BuildRunner.Log($"创建补丁清单版本文件：{filePath}");
			}
		}

		/// <summary>
		/// 获取资源包列表
		/// </summary>
		private List<PatchBundle> GetAllPatchBundle(BuildContext context)
		{
			var buildMapContext = context.GetContextObject<BuildMapContext>();
			var buildParametersContext = context.GetContextObject<BuildParametersContext>();

			List<PatchBundle> result = new List<PatchBundle>(1000);
			foreach (var bundleInfo in buildMapContext.BundleInfos)
			{
				var patchBundle = bundleInfo.CreatePatchBundle();
				result.Add(patchBundle);
			}
			return result;
		}

		/// <summary>
		/// 获取资源列表
		/// </summary>
		private List<PatchAsset> GetAllPatchAsset(BuildContext context, PatchManifest patchManifest, string package, out List<string> bundleNameList, out List<string> dependBundleNameList)
		{
			var buildMapContext = context.GetContextObject<BuildMapContext>();

			List<PatchAsset> result = new List<PatchAsset>(1000);
			 bundleNameList = new List<string>();
			 dependBundleNameList = new List<string>();

			foreach (var bundleInfo in buildMapContext.BundleInfos)
			{
				if (bundleInfo.Package == package)
                {
					var assetInfos = bundleInfo.GetAllPatchAssetInfos();
					foreach (var assetInfo in assetInfos)
					{
						PatchAsset patchAsset = new PatchAsset();
						if (buildMapContext.EnableAddressable)
							patchAsset.Address = assetInfo.Address;
						else
							patchAsset.Address = string.Empty;
						patchAsset.AssetPath = assetInfo.AssetPath;
						patchAsset.AssetTags = assetInfo.AssetTags.ToArray();
						var bundleName = assetInfo.GetBundleName();
						if (bundleNameList.Contains(bundleName))
                        {
							patchAsset.BundleID = bundleNameList.IndexOf(bundleName);
						}
						else
                        {
							bundleNameList.Add(assetInfo.GetBundleName());
							patchAsset.BundleID = bundleNameList.Count - 1;
						}
						patchAsset.DependIDs = GetAssetBundleDependIDs(bundleName, assetInfo, patchManifest,ref dependBundleNameList);
						result.Add(patchAsset);
					}
                }
					
			}
			return result;
		}
		private int[] GetAssetBundleDependIDs(string mainBundleName, BuildAssetInfo assetInfo, PatchManifest patchManifest, ref List<string> dependBundleNameList)
		{
			List<int> result = new List<int>();
			if(dependBundleNameList==null)
				dependBundleNameList = new List<string>();
			if (assetInfo.AllDependAssetInfos != null)
            {
				foreach (var dependAssetInfo in assetInfo.AllDependAssetInfos)
				{
					if (dependAssetInfo.HasBundleName())
					{
						var bundleName = dependAssetInfo.GetBundleName();
                        if (dependBundleNameList.Contains(bundleName))
                        {
							var bundleID = dependBundleNameList.IndexOf(bundleName);
							if (mainBundleName != bundleName && !result.Contains(bundleID))
                            {
								result.Add(bundleID);
							}
						}
                        else
                        {
							dependBundleNameList.Add(bundleName);
							result.Add(dependBundleNameList.Count-1);
						}
					}
				}
			}
			return result.ToArray();
		}
		private int GetAssetBundleID(string bundleName, PatchManifest patchManifest)
		{
			for (int index = 0; index < patchManifest.BundleList.Count; index++)
			{
				if (patchManifest.BundleList[index].BundleName == bundleName)
					return index;
			}
			throw new Exception($"Not found bundle name : {bundleName}");
		}


		/// <summary>
		/// 更新Unity内置资源包的引用关系
		/// </summary>
		private void UpdateBuiltInBundleReference(PatchManifest patchManifest, IBundleBuildResults buildResults, string shadersBunldeName)
		{
			// 获取所有依赖着色器资源包的资源包列表
			List<string> shaderBundleReferenceList = new List<string>();
			foreach (var valuePair in buildResults.BundleInfos)
			{
				if (valuePair.Value.Dependencies.Any(t => t == shadersBunldeName))
					shaderBundleReferenceList.Add(valuePair.Key);
			}

			// 注意：没有任何资源依赖着色器
			if (shaderBundleReferenceList.Count == 0)
				return;

			// 获取着色器资源包索引
			Predicate<PatchBundle> predicate = new Predicate<PatchBundle>(s => s.BundleName == shadersBunldeName);
			var shaderBundle = patchManifest.BundleList.Find(s => s.BundleName == shadersBunldeName);
			if(shaderBundle == null)
				throw new Exception("没有发现着色器资源包！");

			// 检测依赖交集并更新依赖ID
			foreach (var patchAsset in patchManifest.AssetList)
			{
				List<string> dependBundles = GetPatchAssetAllDependBundles(patchManifest, patchAsset);
				List<string> conflictAssetPathList = dependBundles.Intersect(shaderBundleReferenceList).ToList();
				if (conflictAssetPathList.Count > 0)
				{
					List<string> newDependNames = new List<string>();
					for (int i = 0; i < conflictAssetPathList.Count; i++)
                    {
						if (!newDependNames.Contains(conflictAssetPathList[i]) )
							newDependNames.Add(conflictAssetPathList[i]);
                    }
                    
					if (newDependNames.Contains(shaderBundle.BundleName) == false)
						newDependNames.Add(shaderBundle.BundleName);
					var newDependIDs = new List<int>();
					var dependNameList = new List<string>(patchManifest.DependBundleNameList);
					for (int i = 0; i < newDependNames.Count; i++)
                    {
						var id = dependNameList.IndexOf(newDependNames[i]);
						if (!newDependIDs.Contains(id))
                        {
							newDependIDs.Add(id);
						}
					}
					patchAsset.DependIDs = newDependIDs.ToArray();
				}
			}
		}
		private List<string> GetPatchAssetAllDependBundles(PatchManifest patchManifest, PatchAsset patchAsset)
		{
			List<string> result = new List<string>();
			string mainBundle = patchManifest.BundleNameList[patchAsset.BundleID];
			result.Add(mainBundle);
			foreach (var dependID in patchAsset.DependIDs)
			{
				result.Add(patchManifest.DependBundleNameList[dependID]);
			}
			return result;
		}
	}
}