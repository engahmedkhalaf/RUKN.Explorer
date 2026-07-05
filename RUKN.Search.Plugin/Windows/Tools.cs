using Autodesk.Navisworks.Api;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace RUKN.Search.Plugin.Utils
{
    public class Tools
    {
        public static string SelectedIds;
        private static StringBuilder strBuilder;
        public static Document Doc { get; set; }
        public static ModelItemCollection CurrentSelection { get; set; }

        /// <summary>
        /// Convert input string to int
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static List<string> splitString(string s)
        {
            char delimit = ',';
            return s.Split(delimit).ToList();
        }

        internal static void GetIds(ModelItemCollection modelItemCollection)
        {
            foreach (var ModelItem in modelItemCollection)
            {
                if (ModelItem.HasGeometry | ModelItem.IsInsert)
                {
                    ModelItemCollection mic = new ModelItemCollection
                    {
                        ModelItem.Parent
                    };
                    Tools.GetIds(mic);
                    break;
                }
                if (ModelItem.IsCollection && ModelItem.IsLayer)
                {
                    ModelItemCollection mic = new ModelItemCollection();
                    mic.AddRange(ModelItem.Children);
                    Tools.GetIds(mic);
                }
                else
                {
                    var id = ModelItem.PropertyCategories.FindPropertyByName("LcRevitId", "LcOaNat64AttributeValue").Value.ToDisplayString();
                    strBuilder = new StringBuilder();
                    strBuilder.Append(SelectedIds);
                    strBuilder.Append(id);
                    strBuilder.Append(",");
                    SelectedIds = strBuilder.ToString();
                }
            }
        }

        /// <summary>
        /// Get selection from int Revit ID
        /// </summary>
        /// <param name="li"></param>
        /// <returns></returns>
        public static ModelItemCollection getElements(List<string> li)
        {
            // Execute Search
            ModelItemCollection items = new ModelItemCollection();
            global::Autodesk.Navisworks.Api.Search search = new global::Autodesk.Navisworks.Api.Search();
            search.Selection.SelectAll();
            List<string> list = li.Distinct().ToList();
            for (int i = 0; i < list.Count(); i++)
            {
                string st = list[i];
                search.SearchConditions.Add(SearchCondition.HasPropertyByName("LcRevitId", "LcOaNat64AttributeValue").EqualValue(VariantData.FromDisplayString(st)));
                var item = search.FindFirst(Autodesk.Navisworks.Api.Application.ActiveDocument, false);
                if (item != null)
                    items.Add(item);
                search.Clear();
            }
            if (items.Count != list.Count) 
            {
                MessageWindow.Show("Warning", "This is not a valid Id");
            }

            Autodesk.Navisworks.Api.Application.ActiveDocument.CurrentSelection.

                CopyFrom(items);

            return items;
        }
        public static bool IsRevitModelLoaded()
        {
            var activeDoc = Autodesk.Navisworks.Api.Application.ActiveDocument;
            return activeDoc.ActiveSheet != null;
        }
    }
}