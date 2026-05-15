using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.DataCenter;
using OpenSEMI.ClientBase.ServiceProvider;

namespace MECF.Framework.UI.Client.CenterViews.DataLogs.ProcessHistory
{
    public class ProcessHistoryProvider : IProvider
    {
        private static ProcessHistoryProvider _Instance = null;
        public static ProcessHistoryProvider Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = new ProcessHistoryProvider();

                return _Instance;
            }
        }

        public void Create()
        {

        }

        public ObservableCollection<RecipeItem> SearchRecipe()
        {
            ObservableCollection <RecipeItem> result = new ObservableCollection<RecipeItem>();

            for (int i = 0; i < 5; i++)
            {
                RecipeItem r = new RecipeItem() { Chamber = "c " + i.ToString(), Recipe = "recipe " + i.ToString(), Selected = false, Status = "s " + i.ToString(), EndTime = "", StartTime = "" };
                result.Add(r);
            }

            return result;
        }

        public ObservableCollection<ParameterNode> GetParameters1()
        {
            #region Test code
            ObservableCollection<ParameterNode> result = new ObservableCollection<ParameterNode>();

            ParameterNode node1 = new ParameterNode() { Name = "Para Node 1", Selected = false, ChildNodes = new ObservableCollection<ParameterNode>() };
            ParameterNode node2 = new ParameterNode() { Name = "Para Node 2", Selected = false, ChildNodes = new ObservableCollection<ParameterNode>() };
            ParameterNode node3 = new ParameterNode() { Name = "Para Node 3", Selected = false, ChildNodes = new ObservableCollection<ParameterNode>() };

            for (int i = 0; i < 5; i++)
            {
                ParameterNode node = new ParameterNode() { Name = node1.Name+"_"+i.ToString(), Selected = false, ChildNodes = new ObservableCollection<ParameterNode>() };
                node1.ChildNodes.Add(node);
            }

            for (int i = 0; i < 3; i++)
            {
                ParameterNode node = new ParameterNode() { Name = node2.Name + "_" + i.ToString(), Selected = false, ChildNodes = new ObservableCollection<ParameterNode>() };
                node2.ChildNodes.Add(node);
            }

            for (int i = 0; i <4 ; i++)
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


        public ObservableCollection<ParameterNode> GetParameters( )
        {
            try
            {
                List<string> dataList = (List<string>)QueryDataClient.Instance.Service.GetConfig("System.NumericDataList");
                dataList.Sort();

                ObservableCollection<ParameterNode> rootNode = new ObservableCollection<ParameterNode>();
                Dictionary<string, ParameterNode> indexer = new Dictionary<string, ParameterNode>();
                foreach (string dataName in dataList)
                {
 
                    if (!dataName.StartsWith("PM1.") && !dataName.StartsWith("PM2.")
                                                     && !dataName.StartsWith("PM3.") && !dataName.StartsWith("PM4.")
                                                     && !dataName.StartsWith("PM5.") && !dataName.StartsWith("PM6.")
                                                     && !dataName.StartsWith("PM7."))
                    {
                        continue;
                    }

                    string[] nodeName = dataName.Split('.');
                    ParameterNode parentNode = null;
                    string pathName = "";
                    for (int i = 0; i < nodeName.Length; i++)
                    {
                        pathName = (i == 0) ? nodeName[i] : (pathName + "." + nodeName[i]);
                        if (!indexer.ContainsKey(pathName))
                        {
                            indexer[pathName] = new ParameterNode() { Name = pathName, ChildNodes = new ObservableCollection<ParameterNode>() };

                            if (parentNode == null)
                                rootNode.Add(indexer[pathName]);
                            else
                            {
                                parentNode.ChildNodes.Add(indexer[pathName]);
                            }
                        }

                        parentNode = indexer[pathName];
                    }
                }

                return rootNode;
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
