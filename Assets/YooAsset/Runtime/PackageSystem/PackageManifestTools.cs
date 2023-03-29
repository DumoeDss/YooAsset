using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace YooAsset
{
	internal static class PackageManifestTools
	{

#if UNITY_EDITOR
		/// <summary>
		/// 序列化（JSON文件）
		/// </summary>
		public static void SerializeToJson(string savePath, PackageManifest manifest)
		{
			string json = JsonUtility.ToJson(manifest, true);
			FileUtility.CreateFile(savePath, json);
		}

		/// <summary>
		/// 序列化（二进制文件）
		/// </summary>
		public static void SerializeToBinary(string savePath, PackageManifest PackageManifest)
		{
			using (FileStream fs = new FileStream(savePath, FileMode.Create))
			{
				// 创建缓存器
				BufferWriter buffer = new BufferWriter(YooAssetSettings.ManifestFileMaxSize);

				// 写入文件标记
				buffer.WriteUInt32(YooAssetSettings.ManifestFileSign);

				// 写入文件版本
				buffer.WriteUTF8(PackageManifest.FileVersion);

				// 写入文件头信息
				buffer.WriteBool(PackageManifest.EnableAddressable);
				buffer.WriteInt32(PackageManifest.OutputNameStyle);
				buffer.WriteUTF8(PackageManifest.PackageName);
				buffer.WriteUTF8(PackageManifest.PackageVersion);
				buffer.WriteUTF8Array(PackageManifest.BundleNameList);
				buffer.WriteUTF8Array(PackageManifest.DependBundleNameList);
				// 写入资源列表
				buffer.WriteInt32(PackageManifest.AssetList.Count);
				for (int i = 0; i < PackageManifest.AssetList.Count; i++)
				{
					var PackageAsset = PackageManifest.AssetList[i];
					buffer.WriteUTF8(PackageAsset.Address);
					buffer.WriteUTF8(PackageAsset.AssetPath);
					buffer.WriteUTF8Array(PackageAsset.AssetTags);
					buffer.WriteInt32(PackageAsset.BundleID);
					buffer.WriteInt32Array(PackageAsset.DependIDs);
				
				}

				// 写入资源包列表
				buffer.WriteInt32(PackageManifest.BundleList.Count);
				for (int i = 0; i < PackageManifest.BundleList.Count; i++)
				{
					var PackageBundle = PackageManifest.BundleList[i];
					buffer.WriteUTF8(PackageBundle.BundleName);
					buffer.WriteUTF8(PackageBundle.FileHash);
					buffer.WriteUTF8(PackageBundle.FileCRC);
					buffer.WriteInt64(PackageBundle.FileSize);
					buffer.WriteBool(PackageBundle.IsRawFile);
					buffer.WriteByte(PackageBundle.LoadMethod);
					buffer.WriteUTF8Array(PackageBundle.Tags);
                    buffer.WriteInt32Array(PackageBundle.ReferenceIDs);
                }
                buffer.WriteUTF8Array(PackageManifest.AssemblyAddresses);
				buffer.WriteUTF8Array(PackageManifest.DependAssemblyAddresses);
                // 写入文件流
                buffer.WriteToStream(fs);
				fs.Flush();
			}
		}

		/// <summary>
		/// 反序列化（二进制文件）
		/// </summary>
		public static PackageManifest DeserializeFromBinary(byte[] binaryData)
		{
			// 创建缓存器
			BufferReader buffer = new BufferReader(binaryData);

			// 读取文件标记
			uint fileSign = buffer.ReadUInt32();
			if (fileSign != YooAssetSettings.ManifestFileSign)
				throw new Exception("Invalid manifest file !");

			// 读取文件版本
			string fileVersion = buffer.ReadUTF8();
			if (fileVersion != YooAssetSettings.ManifestFileVersion)
				throw new Exception($"The manifest file version are not compatible : {fileVersion} != {YooAssetSettings.ManifestFileVersion}");

			PackageManifest manifest = new PackageManifest();
			{
				// 读取文件头信息
				manifest.FileVersion = fileVersion;
				manifest.EnableAddressable = buffer.ReadBool();
				manifest.OutputNameStyle = buffer.ReadInt32();
				manifest.PackageName = buffer.ReadUTF8();
				manifest.PackageVersion = buffer.ReadUTF8();
				manifest.BundleNameList = buffer.ReadUTF8Array();
				manifest.DependBundleNameList = buffer.ReadUTF8Array();
				// 读取资源列表
				int PackageAssetCount = buffer.ReadInt32();
				manifest.AssetList = new List<PackageAsset>(PackageAssetCount);
				for (int i = 0; i < PackageAssetCount; i++)
				{
					var PackageAsset = new PackageAsset();
					PackageAsset.Address = buffer.ReadUTF8();
					PackageAsset.AssetPath = buffer.ReadUTF8();
					PackageAsset.AssetTags = buffer.ReadUTF8Array();
					PackageAsset.BundleID = buffer.ReadInt32();
					PackageAsset.DependIDs = buffer.ReadInt32Array();
					manifest.AssetList.Add(PackageAsset);
				}

				// 读取资源包列表
				int packageBundleCount = buffer.ReadInt32();
				manifest.BundleList = new List<PackageBundle>(packageBundleCount);
				for (int i = 0; i < packageBundleCount; i++)
				{
					var PackageBundle = new PackageBundle();
					PackageBundle.BundleName = buffer.ReadUTF8();
					PackageBundle.FileHash = buffer.ReadUTF8();
					PackageBundle.FileCRC = buffer.ReadUTF8();
					PackageBundle.FileSize = buffer.ReadInt64();
					PackageBundle.IsRawFile = buffer.ReadBool();
					PackageBundle.LoadMethod = buffer.ReadByte();
					PackageBundle.Tags = buffer.ReadUTF8Array();
					PackageBundle.ReferenceIDs = buffer.ReadInt32Array();
                    manifest.BundleList.Add(PackageBundle);
				}

				manifest.AssemblyAddresses = buffer.ReadUTF8Array();
				manifest.DependAssemblyAddresses = buffer.ReadUTF8Array();
			}

			// BundleDic
			manifest.BundleDic = new Dictionary<string, PackageBundle>(manifest.BundleList.Count);
			foreach (var PackageBundle in manifest.BundleList)
			{
				PackageBundle.ParseBundle(manifest.PackageName, manifest.OutputNameStyle);
				manifest.BundleDic.Add(PackageBundle.BundleName, PackageBundle);
			}

			// AssetDic
			manifest.AssetDic = new Dictionary<string, PackageAsset>(manifest.AssetList.Count);
			foreach (var PackageAsset in manifest.AssetList)
			{
				// 注意：我们不允许原始路径存在重名
				string assetPath = PackageAsset.AssetPath;
				if (manifest.AssetDic.ContainsKey(assetPath))
					throw new Exception($"AssetPath have existed : {assetPath}");
				else
					manifest.AssetDic.Add(assetPath, PackageAsset);
			}

			return manifest;
		}
#endif

		public static string GetRemoteBundleFileExtension(string bundleName)
		{
			string fileExtension = Path.GetExtension(bundleName);
			return fileExtension;
		}
		public static string GetRemoteBundleFileName(int nameStyle, string bundleName, string fileExtension, string fileHash)
		{
			if (nameStyle == 1) //HashName
			{
				return StringUtility.Format("{0}{1}", fileHash, fileExtension);
			}
			else if (nameStyle == 4) //BundleName_HashName
			{
				string fileName = bundleName.Remove(bundleName.LastIndexOf('.'));
				return StringUtility.Format("{0}_{1}{2}", fileName, fileHash, fileExtension);
			}
			else
			{
				throw new NotImplementedException($"Invalid name style : {nameStyle}");
			}
		}

		/// <summary>
		/// 获取解压BundleInfo
		/// </summary>
		public static BundleInfo GetUnpackInfo(PackageBundle PackageBundle)
		{
			// 注意：我们把流加载路径指定为远端下载地址
			string streamingPath = PathHelper.ConvertToWWWPath(PackageBundle.StreamingFilePath);
			BundleInfo bundleInfo = new BundleInfo(PackageBundle, BundleInfo.ELoadMode.LoadFromStreaming, streamingPath, streamingPath);
			return bundleInfo;
		}
	}
}