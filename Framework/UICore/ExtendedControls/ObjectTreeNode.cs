using System.Collections;
using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace MECF.Framework.UI.Core.ExtendedControls
{
    public class ObjectTreeNode
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public List<ObjectTreeNode> Children { get; set; } = new List<ObjectTreeNode>();

        public static ObjectTreeNode CreateTree(object obj)
        {
            JavaScriptSerializer jss = new JavaScriptSerializer();
            var serialized = Newtonsoft.Json.JsonConvert.SerializeObject(obj);
            Dictionary<string, object> dic = jss.Deserialize<Dictionary<string, object>>(serialized);
            var root = new ObjectTreeNode();
            root.Name = "Root";
            BuildTree(dic, root);
            return root;
        }

        private static void BuildTree(object item, ObjectTreeNode node)
        {
            if (item is KeyValuePair<string, object>)
            {
                KeyValuePair<string, object> kv = (KeyValuePair<string, object>) item;
                ObjectTreeNode keyValueNode = new ObjectTreeNode();
                keyValueNode.Name = kv.Key;
                keyValueNode.Value = GetValueAsString(kv.Value);
                node.Children.Add(keyValueNode);
                BuildTree(kv.Value, keyValueNode);
            }
            else if (item is ArrayList)
            {
                ArrayList list = (ArrayList) item;
                int index = 0;
                foreach (object value in list)
                {
                    ObjectTreeNode arrayItem = new ObjectTreeNode();
                    arrayItem.Name = $"[{index}]";
                    arrayItem.Value = "";
                    node.Children.Add(arrayItem);
                    BuildTree(value, arrayItem);
                    index++;
                }
            }
            else if (item is Dictionary<string, object>)
            {
                Dictionary<string, object> dictionary = (Dictionary<string, object>) item;
                foreach (KeyValuePair<string, object> d in dictionary)
                {
                    BuildTree(d, node);
                }
            }
        }

        private static string GetValueAsString(object value)
        {
            if (value == null)
                return "null";
            var type = value.GetType();
            if (type.IsArray)
            {
                return "[]";
            }

            if (value is ArrayList)
            {
                var arr = value as ArrayList;
                return $"[{arr.Count}]";
            }

            if (type.IsGenericType)
            {
                return "{}";
            }

            return value.ToString();
        }
    }
}