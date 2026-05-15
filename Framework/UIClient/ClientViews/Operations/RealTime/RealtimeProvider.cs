using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.UI.Client.CenterViews.DataLogs.ProcessHistory;
using OpenSEMI.ClientBase.ServiceProvider;

namespace MECF.Framework.UI.Client.CenterViews.Operations.RealTime
{
    public class RealtimeProvider : IProvider
    {
        //private static RealtimeProvider _Instance = null;
        //public static RealtimeProvider Instance
        //{
        //    get
        //    {
        //        if (_Instance == null)
        //            _Instance = new RealtimeProvider();

        //        return _Instance;
        //    }
        //}

        ObservableCollection<ParameterNode> _rootNode = new ObservableCollection<ParameterNode>();
        Dictionary<string, ParameterNode> _indexer = new Dictionary<string, ParameterNode>();
        public void Clear()
        {
            _indexer.Clear();
            _rootNode.Clear();
        }
        public void Create()
        {

        }

        public ObservableCollection<string> GetUserDefineParameters()
        {
            var typedContents = ((string)QueryDataClient.Instance.Service.GetTypedConfigContent("UserDefine", ""));
            ObservableCollection<string> dataList = new ObservableCollection<string>();
            if (typedContents != null)
            {
                var contentList = typedContents.Split(',').ToList();
                contentList.ForEach(x => { if (!string.IsNullOrEmpty(x)) dataList.Add($"{x}"); });
            }

            return dataList;
        }

        public ObservableCollection<ParameterNode> GetParameters()
        {
            try
            {
                List<string> dataList = (List<string>)QueryDataClient.Instance.Service.GetConfig("System.NumericDataList");
                var typedContents = ((string)QueryDataClient.Instance.Service.GetTypedConfigContent("UserDefine", ""));
                if (string.IsNullOrEmpty(typedContents))
                {
                    dataList.Add($"UserDefine");
                }
                else
                {
                    var contentList = typedContents.Split(',').ToList();
                    contentList.ForEach(x => { if (!string.IsNullOrEmpty(x)) dataList.Add($"UserDefine.{x}"); });
                }
                dataList.Sort();
                //List<string> removeList = _indexer.Keys.ToList();
                foreach (string dataName in dataList)
                {
                    string[] nodeName = dataName.Split('.');
                    ParameterNode parentNode = null;
                    string pathName="";
                    for (int i = 0; i < nodeName.Length; i++)
                    {
                        pathName = (i == 0) ? nodeName[i] : (pathName + "." + nodeName[i]);

                        //removeList.Remove(pathName);

                        if (!_indexer.ContainsKey(pathName))
                        {
                            _indexer[pathName] = new ParameterNode() { Name = pathName.Replace("UserDefine.", ""), ChildNodes = new ObservableCollection<ParameterNode>(), ParentNode = parentNode};

                            if (parentNode == null)
                            {
                                _rootNode.Add(_indexer[pathName]);
                            }
                            else
                            {
                                parentNode.ChildNodes.Add(_indexer[pathName]);
                            }
                        }

                        parentNode = _indexer[pathName];

                        //removeList.Remove(pathName);
                    }
                }

                //foreach (var key in removeList)
                //{
                //    if (_indexer[key].ParentNode == null)
                //        _rootNode.Remove(_indexer[key]);
                //    else
                //    {
                //        _indexer[key].ParentNode.ChildNodes.Remove(_indexer[key]);
                //    }
                //}

                return _rootNode;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
            

            #region Test code
            ObservableCollection<ParameterNode> result = new ObservableCollection<ParameterNode>();

            ParameterNode node1 = new ParameterNode() { Name = "Para Node 1", Selected = false, ChildNodes = new ObservableCollection<ParameterNode>() };
            ParameterNode node2 = new ParameterNode() { Name = "Para Node 2", Selected = false, ChildNodes = new ObservableCollection<ParameterNode>() };
            ParameterNode node3 = new ParameterNode() { Name = "Para Node 3", Selected = false, ChildNodes = new ObservableCollection<ParameterNode>() };

            for (int i = 0; i < 5; i++)
            {
                ParameterNode node = new ParameterNode() { Name = node1.Name + "_" + i.ToString(), Selected = false, ChildNodes = new ObservableCollection<ParameterNode>() };
                node1.ChildNodes.Add(node);
            }

            for (int i = 0; i < 3; i++)
            {
                ParameterNode node = new ParameterNode() { Name = node2.Name + "_" + i.ToString(), Selected = false, ChildNodes = new ObservableCollection<ParameterNode>() };
                node2.ChildNodes.Add(node);
            }

            for (int i = 0; i < 4; i++)
            {
                ParameterNode node = new ParameterNode() { Name = node3.Name + "_" + i.ToString(), Selected = false, ChildNodes = new ObservableCollection<ParameterNode>() };
                node3.ChildNodes.Add(node);
            }

            result.Add(node1);
            result.Add(node2);
            result.Add(node3);
            return result;
            #endregion
        }
    }
}
