﻿using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace YooAsset.Editor
{
	public static class BuildMapCreater
	{
		/// <summary>
		/// 执行资源构建上下文
		/// </summary>
		public static BuildMapContext CreateBuildMap(EBuildMode buildMode, List<string> packageNames)
		{
			BuildMapContext context = new BuildMapContext();
			Dictionary<string, BuildAssetInfo> buildAssetDic = new Dictionary<string, BuildAssetInfo>(1000);

			// 1. 检测配置合法性
			AssetBundleCollectorSettingData.Setting.CheckConfigError();

			// 2. 获取所有收集器收集的资源
			var buildResult = AssetBundleCollectorSettingData.Setting.GetPackageAssets(buildMode, packageNames);
			List<CollectAssetInfo> allCollectAssets = new List<CollectAssetInfo>();
			foreach (var item in buildResult)
            {
				allCollectAssets.AddRange(item.CollectAssets);
			}

			// 3. 剔除未被引用的依赖资源
			List<CollectAssetInfo> removeDependList = new List<CollectAssetInfo>();
			foreach (var collectAssetInfo in allCollectAssets)
			{
				if (collectAssetInfo.CollectorType == ECollectorType.DependAssetCollector)
				{
					if (IsRemoveDependAsset(allCollectAssets, collectAssetInfo.AssetPath))
						removeDependList.Add(collectAssetInfo);
				}
			}
			foreach (var removeValue in removeDependList)
			{
				allCollectAssets.Remove(removeValue);
			}

			// 4. 录入所有收集器收集的资源
			foreach (var collectAssetInfo in allCollectAssets)
			{
				if (buildAssetDic.ContainsKey(collectAssetInfo.AssetPath) == false)
				{
					var buildAssetInfo = new BuildAssetInfo(collectAssetInfo.CollectorType, collectAssetInfo.PackageName, collectAssetInfo.IncludeInBuild,
						collectAssetInfo.BundleName,
						collectAssetInfo.Address, collectAssetInfo.AssetPath, collectAssetInfo.IsRawAsset, collectAssetInfo.IsAssemblyAsse);
					buildAssetInfo.AddAssetTags(collectAssetInfo.AssetTags);
					buildAssetInfo.AddBundleTags(collectAssetInfo.AssetTags);
					buildAssetDic.Add(collectAssetInfo.AssetPath, buildAssetInfo);
				}
				else
				{
					throw new Exception($"Should never get here !");
				}
			}

			// 5. 录入相关依赖的资源
			foreach (var collectAssetInfo in allCollectAssets)
			{
				foreach (var dependAssetPath in collectAssetInfo.DependAssets)
				{
					if (buildAssetDic.ContainsKey(dependAssetPath))
					{
						buildAssetDic[dependAssetPath].AddBundleTags(collectAssetInfo.AssetTags);
						buildAssetDic[dependAssetPath].AddReferenceBundleName(collectAssetInfo.BundleName);
					}
					else
					{
						var buildAssetInfo = new BuildAssetInfo(dependAssetPath, collectAssetInfo.PackageName, collectAssetInfo.IncludeInBuild);
						buildAssetInfo.AddBundleTags(collectAssetInfo.AssetTags);
						buildAssetInfo.AddReferenceBundleName(collectAssetInfo.BundleName);
						buildAssetDic.Add(dependAssetPath, buildAssetInfo);
					}
				}
			}

			// 6. 记录关键信息
			context.AssetFileCount = buildAssetDic.Count;
			context.EnableAddressable = AssetBundleCollectorSettingData.Setting.EnableAddressable;
			context.UniqueBundleName = AssetBundleCollectorSettingData.Setting.UniqueBundleName;

			// 7. 填充主动收集资源的依赖列表
			foreach (var collectAssetInfo in allCollectAssets)
			{
				var dependAssetInfos = new List<BuildAssetInfo>(collectAssetInfo.DependAssets.Count);
				foreach (var dependAssetPath in collectAssetInfo.DependAssets)
				{
					if (buildAssetDic.TryGetValue(dependAssetPath, out BuildAssetInfo value))
                    {
						if (value.PackageName != collectAssetInfo.PackageName)
						{
							dependAssetInfos.Add(new BuildAssetInfo(value));
						}
						else
						{
							dependAssetInfos.Add(value);
						}
					}
					else
						throw new Exception("Should never get here !");
				}
				buildAssetDic[collectAssetInfo.AssetPath].SetAllDependAssetInfos(dependAssetInfos);
			}

			// 8. 计算完整的资源包名
			foreach (KeyValuePair<string, BuildAssetInfo> pair in buildAssetDic)
			{
				pair.Value.CalculateFullBundleName(AssetBundleCollectorSettingData.Setting.UniqueBundleName);
			}

			// 9. 移除不参与构建的资源
			List<BuildAssetInfo> removeBuildList = new List<BuildAssetInfo>();
			foreach (KeyValuePair<string, BuildAssetInfo> pair in buildAssetDic)
			{
				var buildAssetInfo = pair.Value;
				if (buildAssetInfo.HasBundleName() == false)
					removeBuildList.Add(buildAssetInfo);
			}
			foreach (var removeValue in removeBuildList)
			{
				buildAssetDic.Remove(removeValue.AssetPath);
			}

			// 10. 构建资源包
			var allBuildinAssets = buildAssetDic.Values.ToList();
			if (allBuildinAssets.Count == 0)
				throw new Exception("构建的资源列表不能为空");
			foreach (var assetInfo in allBuildinAssets)
			{
				context.PackAsset(assetInfo);
			}
			return context;
		}
		private static bool IsRemoveDependAsset(List<CollectAssetInfo> allCollectAssets, string dependAssetPath)
		{
			foreach (var collectAssetInfo in allCollectAssets)
			{
				var collectorType = collectAssetInfo.CollectorType;
				if (collectorType == ECollectorType.MainAssetCollector || collectorType == ECollectorType.StaticAssetCollector)
				{
					if (collectAssetInfo.DependAssets.Contains(dependAssetPath))
						return false;
				}
			}

			BuildRunner.Log($"发现未被依赖的资源并自动移除 : {dependAssetPath}");
			return true;
		}
	}
}