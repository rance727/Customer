using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.JSON;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.ComponentModel;
using System.Data;
using System.Xml;

namespace BHM.SLM.SCP.GYSPT.BDCJ
{
    [Description("供应商平台")]
    //[Kingdee.BOS.Util.HotUpdate]
    public class SUPPLIERPT : AbstractBillPlugIn
    {
        public static String ReadXML(string node, string attribute)
        {
            string str = "";
            try
            {
                string path = GetPath();
                XmlDocument document = new XmlDocument();
                document.Load(path);

                XmlNode node2 = document.SelectSingleNode(node);
                XmlElement xe = (XmlElement)node2;
                str = node2.SelectSingleNode(attribute).FirstChild.InnerText.ToString();
            }
            catch
            {
            }
            return str;
        }

        public static String GetPath()
        {
            string fullAppPath = System.Environment.CurrentDirectory;
            return (fullAppPath) + "\\SupplierURL.xml";
            //return "";
        }

        /// <summary>        
        /// /// 主菜单点击事件        
        /// /// </summary>        
        /// /// <param name="e"></param>        
        public override void AfterButtonClick(AfterButtonClickEventArgs e)
        {
            base.AfterButtonClick(e);
            if (e.Key.Equals("bhm_tbButton_GYSPT"))
            {
                var userid = this.Context.UserId;
                var language = this.Context.LogLocale.LCID;
                string sql = @" select t1.FNUMBER from T_SCP_USERDATA t
inner join T_BD_SUPPLIER t1 on t1.FSUPPLIERID = t.FSUPPLIERID
where t.fuserid = " + userid + "";

                DataTable dt = DBUtils.ExecuteDataSet(this.Context, sql).Tables[0];
                if (dt.Rows.Count > 0)
                {
                    string suppliernumber = dt.Rows[0][0].ToString();
                    string url = ReadXML("Supplier", "URL");
                    url = url + "?acc=" + Encryption64.Encrypt(suppliernumber, "!#$a54?3") + "&lang=" + Encryption64.Encrypt(language.ToString(), "!#$a54?3") + "&date=" + Encryption64.Encrypt(DateTime.Now.ToLongDateString(), "!#$a54?3") + "&time=" + Encryption64.Encrypt(DateTime.Now.ToString("HH:mm:ss"), "!#$a54?3") + "&resc=2";
                    ViewCommonAction.ShowWebURL(this.View, url);
                    this.View.SendDynamicFormAction(this.View);
                }
                else
                {
                    this.View.ShowMessage("当前用户未绑定供应商，请联系系统管理员！");
                }
            }
        }

        public override void AfterBindData(EventArgs e)
        {
            base.AfterBindData(e);
            // 方式一                
            // 直接打开浏览器并跳转到此Url地址
            var userid = this.Context.UserId;
            var language = this.Context.LogLocale.LCID;
            string sql = @" select t1.FNUMBER from T_SCP_USERDATA t
inner join T_BD_SUPPLIER t1 on t1.FSUPPLIERID = t.FSUPPLIERID
where t.fuserid = " + userid + "";

            DataTable dt = DBUtils.ExecuteDataSet(this.Context, sql).Tables[0];

            if (dt.Rows.Count > 0)
            {
                string suppliernumber = dt.Rows[0][0].ToString();
                string url = ReadXML("Supplier", "URL");
                url = url + "?acc=" + Encryption64.Encrypt(suppliernumber, "!#$a54?3") + "&lang=" + Encryption64.Encrypt(language.ToString(), "!#$a54?3") + "&date=" + Encryption64.Encrypt(DateTime.Now.ToLongDateString(), "!#$a54?3") + "&time=" + Encryption64.Encrypt(DateTime.Now.ToString("HH:mm:ss"), "!#$a54?3") + "&resc=2";
                ViewCommonAction.ShowWebURL(this.View, url);
                this.View.SendDynamicFormAction(this.View);
            }
            else
            {
                this.View.ShowMessage("当前用户未绑定供应商，请联系系统管理员！");
            }
        }


        public override void AfterBarItemClick(AfterBarItemClickEventArgs e)
        {
            base.AfterBarItemClick(e);
            if (e.BarItemKey.Equals("bhm_tbButton_GYSPT"))
            {
                var userid = this.Context.UserId;
                var language = this.Context.LogLocale.LCID;
                string sql = @" select t1.FNUMBER from T_SCP_USERDATA t
inner join T_BD_SUPPLIER t1 on t1.FSUPPLIERID = t.FSUPPLIERID
where t.fuserid = " + userid + "";

                DataTable dt = DBUtils.ExecuteDataSet(this.Context, sql).Tables[0];

                if (dt.Rows.Count > 0)
                {
                    string suppliernumber = dt.Rows[0][0].ToString();
                    string url = ReadXML("Supplier", "URL");
                    url = url + "?acc=" + Encryption64.Encrypt(suppliernumber, "!#$a54?3") + "&lang=" + Encryption64.Encrypt(language.ToString(), "!#$a54?3") + "&date=" + Encryption64.Encrypt(DateTime.Now.ToLongDateString(), "!#$a54?3") + "&time=" + Encryption64.Encrypt(DateTime.Now.ToString("HH:mm:ss"), "!#$a54?3") + "&resc=2";
                    ViewCommonAction.ShowWebURL(this.View, url);
                    this.View.SendDynamicFormAction(this.View);
                }
                else
                {
                    this.View.ShowMessage("当前用户未绑定供应商，请联系系统管理员！");
                }
            }
        }
    }
}