using Nacos.V2;
using Nacos.V2.Config.Abst;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Nacos.ConfigFilter.Reference
{
    /// <summary>
    /// 
    /// </summary>
    public static class NacosReferenceConfigHandler
    {
        private static readonly IDictionary<string, string> _data
            = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private static INacosConfigurationParser _parser = DefaultJsonConfigurationStringParser.Instance;

        private static readonly HashSet<string> _baseGroups = new HashSet<string>();
        private static readonly HashSet<string> _baseDataIds = new HashSet<string>();

        /// <summary>
        /// 设置默认解析器，与 configuration 一致,  默认会用  JSON 解析器
        /// </summary>
        /// <param name="parser"></param>
        public static void SetParser(INacosConfigurationParser parser)
        {
            _parser = parser;
        }

        public static void SetBaseGroupList(params string[] groups)
        {
            _baseGroups.Clear();
            foreach (var group in groups)
            {
                _baseGroups.Add(group);
            }
        }

        public static void SetBaseDataIdList(params string[] dataIds)
        {
            _baseDataIds.Clear();
            foreach (var dataId in dataIds)
            {
                _baseDataIds.Add(dataId);
            }
        }

        /// <summary>
        /// 加入配置
        /// </summary>
        /// <param name="dataId"></param>
        /// <param name="content"></param>
        internal static void AddYamlContent(object group, object dataId, object content)
        {
            var input = Convert.ToString(content);
            var dataIdStr = Convert.ToString(dataId);
            var groupStr = Convert.ToString(group);

            if (string.IsNullOrWhiteSpace(dataIdStr)) return;
            if (string.IsNullOrEmpty(input)) return;
            if (string.IsNullOrEmpty(groupStr)) return;

            // 如果设置了基础的 base group，然后传入的 group 不存在，就忽略
            // 注意这里区分大小写
            if (_baseGroups.Any())
            {
                if (!_baseGroups.Contains(groupStr))
                {
                    return;
                }
            }

            // 如果设置了基础 dataid，然后传入的dataid不存在，就忽略
            // 注意这里区分大小写
            if (_baseDataIds.Any())
            {
                if (!_baseDataIds.Contains(dataIdStr))
                {
                    return;
                }
            }

            try
            {
                var obj = _parser.Parse(input);

                // 后加入的优先
                foreach (var item in obj)
                {
                    if (_data.ContainsKey(item.Key))
                    {
                        _data.Remove(item.Key);
                    }

                    _data.Add(item);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Parse content failed for dataid: {group}:{dataId}, error: {ex.Message}");
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// 尝试格式化，替换模板
        /// </summary>
        /// <param name="content"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        internal static bool TryFormat(object dataId, object content, out string result)
        {
            var input = Convert.ToString(content);
            result = input;
            if (string.IsNullOrEmpty(input)) return false;

            // 处理模板语法，例如 ${key}, key 格式 AAAAA.BBBB.CCCC
            var pattern = @"\$\{[a-zA-Z0-9\.].*?\}";

            var keysToUpdate = new HashSet<string>();

            var matchResults = Regex.Matches(input, pattern);

            if (!matchResults.Any())
            {
                return false;
            }

            foreach (Match match in matchResults)
            {
                var key = match.Value.Substring(2, match.Value.Length - 3);
                keysToUpdate.Add(key);
            }

            foreach (var key in keysToUpdate)
            {
                var dicKey = key.Replace(".", ":"); // _data 里的 key 是 a:b:c 格式，不区分大小写
                if (_data.TryGetValue(dicKey, out var value))
                {
                    input = input.Replace($"${{{key}}}", value);
                }
                else
                {
                    Console.Error.WriteLine($"Can't find referenced config value with key: DataId: {dataId}, Key: \"${{{key}}}\"");
                    Environment.Exit(0);
                }
            }

            result = input;
            return true;
        }
    }

    /// <summary>
    /// Nacos config filter，用于处理配置引用问题
    /// </summary>
    public class NacosReferenceConfigFilter : IConfigFilter
    {
        public void DoFilter(IConfigRequest request, IConfigResponse response, IConfigFilterChain filterChain)
        {
            if (response != null)
            {
                // 直接将所有配置无脑放到一个 Configuration Builder 内
                var content = response.GetParameter(V2.Config.ConfigConstants.CONTENT);
                var dataId = response.GetParameter(V2.Config.ConfigConstants.DATA_ID);
                var group = response.GetParameter(V2.Config.ConfigConstants.GROUP);

                if (content != null)
                {
                    NacosReferenceConfigHandler.AddYamlContent(group, dataId, content);

                    if (NacosReferenceConfigHandler.TryFormat(dataId, content, out var result))
                    {
                        response.PutParameter(V2.Config.ConfigConstants.CONTENT, result);
                    }
                    else
                    {
                        response.PutParameter(V2.Config.ConfigConstants.CONTENT, content);
                    }
                }

            }
        }

        public string GetFilterName()
        {
            return nameof(NacosReferenceConfigFilter);
        }

        public int GetOrder()
        {
            return 1;
        }

        public void Init(NacosSdkOptions options)
        {
            // pass
            // 可以通过  options.ConfigFilterExtInfo 传入一些额外配置，例如定义哪些是基础配置
        }
    }
}
