﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace YooAsset.Editor
{
	public class BuildAssetInfo
	{
		private string _mainBundleName;
		private string _shareBundleName;
		private bool _isAddAssetTags = false;
		private readonly HashSet<string> _referenceBundleNames = new HashSet<string>();

		/// <summary>
		/// 收集器类型
		/// </summary>
		public ECollectorType CollectorType { private set; get; }

		/// <summary>
		/// 可寻址地址
		/// </summary>
		public string Address { private set; get; }

		/// <summary>
		/// 资源路径
		/// </summary>
		public string AssetPath { private set; get; }

		/// <summary>
		/// 是否为原生资源
		/// </summary>
		public bool IsRawAsset { private set; get; }

		/// <summary>
		/// 是否为着色器资源
		/// </summary>
		public bool IsShaderAsset { private set; get; }

		/// <summary>
		/// 是否为动态库资源
		/// </summary>
		public bool IsAssemblyAsset { private set; get; }

		/// <summary>
		/// 资源的分类标签
		/// </summary>
		public readonly List<string> AssetTags = new List<string>();

		/// <summary>
		/// 资源包的分类标签
		/// </summary>
		public readonly List<string> BundleTags = new List<string>();

		/// <summary>
		/// 依赖的所有资源
		/// 注意：包括零依赖资源和冗余资源（资源包名无效）
		/// </summary>
		public List<BuildAssetInfo> AllDependAssetInfos { private set; get; }

		/// <summary>
		/// 包名
		/// </summary>
		public string PackageName { private set; get; }

		public bool IncludeInBuild { private set; get; }

		public BuildAssetInfo(BuildAssetInfo clone)
		{
			PackageName = clone.PackageName;
			_mainBundleName = $"{PackageName}@{clone._mainBundleName}";
			_shareBundleName = $"{PackageName}@{clone._shareBundleName}";
			CollectorType = clone.CollectorType;
			Address = clone.Address;
			IncludeInBuild = clone.IncludeInBuild;
			AssetPath = clone.AssetPath;
			IsRawAsset = clone.IsRawAsset;
			IsShaderAsset = clone.IsShaderAsset;
			if (clone.AllDependAssetInfos != null)
				AllDependAssetInfos = new List<BuildAssetInfo>(clone.AllDependAssetInfos);
			AssetTags = new List<string>(clone.AssetTags);
			BundleTags = new List<string>(clone.BundleTags);
			_referenceBundleNames = new HashSet<string>(clone._referenceBundleNames);
		}


		public BuildAssetInfo(ECollectorType collectorType, string packageName, bool includeInBuild, string mainBundleName, string address, string assetPath, bool isRawAsset, bool isAssemblyAsset)
		{
			_mainBundleName = mainBundleName;
			CollectorType = collectorType;
			Address = address;
			PackageName = packageName;
			IncludeInBuild = includeInBuild;
			AssetPath = assetPath;
			IsRawAsset = isRawAsset;
			IsAssemblyAsset = isAssemblyAsset;

			System.Type assetType = UnityEditor.AssetDatabase.GetMainAssetTypeAtPath(assetPath);
			if (assetType == typeof(UnityEngine.Shader) || assetType == typeof(UnityEngine.ShaderVariantCollection))
				IsShaderAsset = true;
			else
				IsShaderAsset = false;
		}
		public BuildAssetInfo(string assetPath, string packageName, bool includeInBuild)
		{
			CollectorType = ECollectorType.None;
			Address = string.Empty;
			PackageName = packageName;
			IncludeInBuild = includeInBuild;
			AssetPath = assetPath;
			IsRawAsset = false;
			IsAssemblyAsset = false;

			System.Type assetType = UnityEditor.AssetDatabase.GetMainAssetTypeAtPath(assetPath);
			if (assetType == typeof(UnityEngine.Shader) || assetType == typeof(UnityEngine.ShaderVariantCollection))
				IsShaderAsset = true;
			else
				IsShaderAsset = false;
		}


		/// <summary>
		/// 设置所有依赖的资源
		/// </summary>
		public void SetAllDependAssetInfos(List<BuildAssetInfo> dependAssetInfos)
		{
			if (AllDependAssetInfos != null)
				throw new System.Exception("Should never get here !");

			AllDependAssetInfos = dependAssetInfos;
		}

		/// <summary>
		/// 添加资源的分类标签
		/// 说明：原始定义的资源分类标签
		/// </summary>
		public void AddAssetTags(List<string> tags)
		{
			if (_isAddAssetTags)
				throw new Exception("Should never get here !");
			_isAddAssetTags = true;

			foreach (var tag in tags)
			{
				if (AssetTags.Contains(tag) == false)
				{
					AssetTags.Add(tag);
				}
			}
		}

		/// <summary>
		/// 添加资源包的分类标签
		/// 说明：传染算法统计到的分类标签
		/// </summary>
		public void AddBundleTags(List<string> tags)
		{
			foreach (var tag in tags)
			{
				if (BundleTags.Contains(tag) == false)
				{
					BundleTags.Add(tag);
				}
			}
		}

		/// <summary>
		/// 资源包名是否存在
		/// </summary>
		public bool HasBundleName()
		{
			string bundleName = GetBundleName();
			if (string.IsNullOrEmpty(bundleName))
				return false;
			else
				return true;
		}

		/// <summary>
		/// 获取资源包名称
		/// </summary>
		public string GetBundleName()
		{
			if (CollectorType == ECollectorType.None)
				return _shareBundleName;
			else
				return _mainBundleName;
		}

		/// <summary>
		/// 添加关联的资源包名称
		/// </summary>
		public void AddReferenceBundleName(string bundleName)
		{
			if (string.IsNullOrEmpty(bundleName))
				throw new Exception("Should never get here !");

			if (_referenceBundleNames.Contains(bundleName) == false)
				_referenceBundleNames.Add(bundleName);
		}

		/// <summary>
		/// 计算主资源或共享资源的完整包名
		/// </summary>
		public void CalculateFullBundleName(bool uniqueBundleName)
		{
			if (CollectorType == ECollectorType.None)
			{
				if (IsRawAsset)
					throw new Exception("Should never get here !");

				if (IsShaderAsset)
				{
					_shareBundleName = YooAssetSettingsData.GetUnityShadersBundleFullName(uniqueBundleName, PackageName);
				}
				else
				{
					if (_referenceBundleNames.Count > 0)
					{
						IPackRule packRule = PackDirectory.StaticPackRule;
						var bundleName = packRule.GetBundleName(new PackRuleData(AssetPath));
						if (YooAssetSettingsData.Setting.RegularBundleName)
							bundleName = EditorTools.GetRegularPath(bundleName).Replace('/', '_').Replace('.', '_').ToLower();
						else
							bundleName = EditorTools.GetRegularPath(bundleName).ToLower();
						string prefix = "share";
						if (_referenceBundleNames.Count == 1)
                        {
							prefix = "auto_dependencies";
                        }
						if (uniqueBundleName)
							_shareBundleName = $"{PackageName.ToLower()}_{prefix}_{bundleName}.{YooAssetSettingsData.Setting.AssetBundleFileVariant}";
						else
							_shareBundleName = $"{prefix}_{bundleName}.{YooAssetSettingsData.Setting.AssetBundleFileVariant}";
					}
				}
			}
			else
			{
				if (IsRawAsset)
				{
					string mainBundleName = $"{_mainBundleName}.{YooAssetSettingsData.Setting.RawBundleFileVariant}";
					_mainBundleName = mainBundleName.ToLower();
				}
				else
				{
					string mainBundleName = $"{_mainBundleName}.{YooAssetSettingsData.Setting.AssetBundleFileVariant}";
					_mainBundleName = mainBundleName.ToLower(); ;
				}

				if (uniqueBundleName)
				{
					_mainBundleName = $"{PackageName.ToLower()}_{_mainBundleName}";
				}
			}
		}
	}
}