using Kingdee.BOS.App.Core;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.Bill.PlugIn.Args;
using Kingdee.BOS.Core.Metadata.FormElement;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace LZH.SLMK3.SAL.FHTZ.GETBILLNO
{
    public class GetCusBillNo : AbstractBillPlugIn
    {
        public string strTrueFbillno = "";
        public override void BeforeSave(BeforeSaveEventArgs e)
        {
            base.BeforeSave(e);

            DynamicObject dyObj = this.Model.GetValue("FCustomerID") as DynamicObject;

            string strRulesID = dyObj["FBillNoRules_Id"].ToString();

            string strDeliveryNo = this.View.Model.GetValue("FDeliveryNo") == null ? "" : this.Model.GetValue("FDeliveryNo").ToString(); 

            string strFNumber = this.View.Model.GetValue("FBillNo") == null ? "" : this.Model.GetValue("FBillNo").ToString();

            Dictionary<string, object> dctBillNoOption = new Dictionary<string, object>();
            dctBillNoOption["CodeTime"] = 0;

            dctBillNoOption["UpdateMaxNum"] = 1;

            if (strRulesID != "" && strDeliveryNo =="")
            {
                var billNos = BusinessDataServiceHelper.GetBillNo(this.Context, this.View.BillBusinessInfo, new[] { this.Model.DataObject }, dctBillNoOption, strRulesID);
                               
                this.View.Model.SetValue("FDeliveryNo", billNos[0].BillNo);                
            }

            if (strFNumber == "")
            {
                var billNoField = this.View.BillBusinessInfo.GetBillNoField();
                var options = new Dictionary<string, object>(); options["CodeTime"] = 1;
                // 执行时机，0表示新增时获取单据编号，1表示保存时获取单据编号                
                options["UpdateMaxNum"] = 1; // 是否更新最大流水号                
                this.Model.DataObject[billNoField.PropertyName] = "";
                var billNos = BusinessDataServiceHelper.GetBillNo(this.Context, this.View.BillBusinessInfo, new[] { this.Model.DataObject }, options);
                this.View.UpdateView(billNoField.Key);
            }
            else
            {
                strTrueFbillno = strFNumber;
            }

            this.View.UpdateView();
        }
        public override void AfterSave(AfterSaveEventArgs e)
        {
            base.AfterSave(e);

            if (strTrueFbillno != "")
            {
                this.View.Model.SetValue("FBillNo", strTrueFbillno);

                this.View.UpdateView();
            }
        }
    }
}