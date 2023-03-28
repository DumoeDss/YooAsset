using System.IO;

namespace YooAsset.Editor
{
	[DisplayName("定位地址: 文件名")]
	public class AddressByFileName : IAddressRule
	{
		string IAddressRule.GetAssetAddress(AddressRuleData data)
		{
			return Path.GetFileNameWithoutExtension(data.AssetPath);
		}
	}

	[DisplayName("定位地址: 文件路径")]
	public class AddressByFilePath : IAddressRule
	{
		string IAddressRule.GetAssetAddress(AddressRuleData data)
		{
			return data.AssetPath;
		}
	}

	[DisplayName("定位地址: 分组名+文件名")]
	public class AddressByGroupAndFileName : IAddressRule
	{
		string IAddressRule.GetAssetAddress(AddressRuleData data)
		{
			string fileName = Path.GetFileNameWithoutExtension(data.AssetPath);
			return $"{data.GroupName}_{fileName}";
		}
	}


	[DisplayName("定位地址: Address+文件路径")]
	public class AddressByAddressAndFilePath : IAddressRule
	{
		string IAddressRule.GetAssetAddress(AddressRuleData data)
		{
			if (Path.HasExtension(data.CollectPath))
			{
				return data.Address;
			}
			else
			{
				string path = data.AssetPath.Replace(data.CollectPath, "");
				if (data.IsMultiPlatform)
				{
					string platform = "Windows";
#if UNITY_ANDROID
					platform = "Android";
#elif UNITY_IOS
					platform = "iOS";
#elif UNITY_STANDALONE_OSX
					platform = "OSX";
#endif
					path = path.Replace($"{platform}/", "");
				}
				string fileName = Path.GetFileName(data.AssetPath);
				return $"{data.Address}{path}";
			}
        }
    }

    [DisplayName("定位地址: 文件夹名+文件名")]
	public class AddressByFolderAndFileName : IAddressRule
	{
		string IAddressRule.GetAssetAddress(AddressRuleData data)
		{
			string fileName = Path.GetFileNameWithoutExtension(data.AssetPath);
			FileInfo fileInfo = new FileInfo(data.AssetPath);
			return $"{fileInfo.Directory.Name}_{fileName}";
		}
	}
}