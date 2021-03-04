using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//引用
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS;

//热启动,不用重启IIS,引用
using System.ComponentModel;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using System.Data;
using Kingdee.BOS.App.Data;

namespace LZH.GFK3.SCGL.DCFL
{

    public class Count : AbstractBillPlugIn
    {

        [Description("汇总")]

        public override void EntryBarItemClick(BarItemClickEventArgs e)
        {
            base.EntryBarItemClick(e);

            if (e.BarItemKey.Equals("LZH_Count"))
            {
                this.View.Model.SetValue("F_LZH_Remarks", "您好");
                DynamicObject BillObj = this.Model.DataObject;

                DynamicObjectCollection EntryRows = BillObj["FEntity"] as DynamicObjectCollection;

                DynamicObject DObjWorkshop = BillObj["WorkShopID"] as DynamicObject;

                string strWorkShopNumber = DObjWorkshop["Number"].ToString();

                DataTable dtMaterial = new DataTable();

                dtMaterial.Columns.Add("物料内码", typeof(long));
                dtMaterial.Columns.Add("数量", typeof(System.Decimal));

                bool bolIsin = false;
                int introw = 0;
                string strMaterialIDs = "'0',";
                foreach (DynamicObject EntryObj in EntryRows)
                {
                    long strMaterialID = Convert.ToInt64(EntryObj["FMaterialID_id"]);
                    decimal decQTY = Convert.ToDecimal(EntryObj["FQTY"]);

                    for (int i = 0; i < dtMaterial.Rows.Count; i++)
                    {
                        if (strMaterialID == Convert.ToInt64(dtMaterial.Rows[i]["物料内码"]))
                        {
                            bolIsin = true;
                            introw = i;
                        }
                    }
                    if (bolIsin)
                    {
                        dtMaterial.Rows[introw]["数量"] = Convert.ToDecimal(dtMaterial.Rows[introw]["数量"]) + decQTY;

                    }
                    else
                    {
                        DataRow dr = dtMaterial.NewRow();
                        dr["物料内码"] = strMaterialID;
                        dr["数量"] = decQTY;
                        dtMaterial.Rows.Add(dr);
                        strMaterialIDs += "'" + strMaterialID + "',";
                    }
                }
                strMaterialIDs += "'0'";
                this.View.Model.DeleteEntryData("F_LZH_BackItemCounEntry");

                string strSQL = @"/*dialect*/select t.FMATERIALID 物料内码, t.FBASEUNITID 单位, 
sum(cast(case when FMustQty - FSelPickedQty + FGoodReturnQty + FINCDefectReturnQty - FReturnQty < 0 then 0 else FMustQty - FSelPickedQty + FGoodReturnQty + FINCDefectReturnQty - FReturnQty end as float)) 已分配量,
cast(isnull(t6.FBASEQTY,0) as float) 即时库存, cast(isnull(t7.FINCREASEQTY,0) as float) 最小包装量
from T_PRD_PPBOMENTRY t
inner join T_PRD_PPBOM t1 on t1.FID = t.FID
inner join T_PRD_MOENTRY t2 on t2.FENTRYID = t1.FMOENTRYID
inner join T_PRD_MOENTRY_A t3 on t3.FENTRYID = t2.FENTRYID and t3.FSTATUS in (3, 4, 5)
inner join T_BD_DEPARTMENT t4 on t4.FDEPTID = t2.FWORKSHOPID
inner join T_PRD_PPBOMENTRY_Q t5 on t5.FENTRYID = t.FENTRYID
left join (select t5.FMATERIALID, sum(t.FBASEQTY) FBASEQTY
			from T_STK_INVENTORY t
			inner join T_BD_STOCK t1 on t1.FSTOCKID = t.FSTOCKID
			inner join T_BD_DEPARTMENT t2 on t2.FWIPStockID = t1.FSTOCKID
			inner join T_ORG_ORGANIZATIONS t3 on t3.FORGID = t.FSTOCKORGID
			inner join T_BD_MATERIAL t4 on t4.FMATERIALID = t.FMATERIALID
			inner join T_BD_MATERIAL t5 on t5.FNUMBER = t4.FNUMBER and t5.FUSEORGID = t3.FORGID
			where 1=1 
			and t2.FNUMBER = '" + strWorkShopNumber + @"'
			group by t5.FMATERIALID
			) t6 on t6.FMATERIALID = t.FMATERIALID
inner join T_BD_MATERIALPLAN t7 on t7.FMATERIALID = t.FMATERIALID
where 1=1
and t4.FNUMBER = '"+ strWorkShopNumber + @"'
and t.FMATERIALID in ("+ strMaterialIDs + @")
group by  t.FMATERIALID, t.FBASEUNITID, t6.FBASEQTY, t7.FINCREASEQTY
";
                DataTable dtSQL= DBUtils.ExecuteDataSet(this.Context, strSQL).Tables[0];

                decimal decQTYALL = 0;

                for (int i = 0; i < dtSQL.Rows.Count; i++)
                {
                    this.View.Model.CreateNewEntryRow("F_LZH_BackItemCounEntry");

                    this.View.Model.SetValue("FCMaterialID", dtSQL.Rows[i]["物料内码"].ToString(), i);
                    this.View.InvokeFieldUpdateService("FCMaterialID", i);
                    this.View.Model.SetValue("FCUnitID", dtSQL.Rows[i]["单位"].ToString(), i);

                    for (int j = 0; j < dtMaterial.Rows.Count; j++)
                    {
                        if (dtMaterial.Rows[j]["物料内码"].ToString() == dtSQL.Rows[i]["物料内码"].ToString())
                        {
                            decQTYALL = Convert.ToDecimal(dtMaterial.Rows[j]["数量"]);
                        }
                    }
                    decimal decMustQty = Convert.ToDecimal(dtSQL.Rows[i]["已分配量"]) - Convert.ToDecimal(dtSQL.Rows[i]["即时库存"]) < 0 ? 0 : Convert.ToDecimal(dtSQL.Rows[i]["已分配量"]) - Convert.ToDecimal(dtSQL.Rows[i]["即时库存"]);
                    decimal decFMustQtyPack = 0;
                    if (Convert.ToDecimal(dtSQL.Rows[i]["最小包装量"]) == 0)
                        decFMustQtyPack = decMustQty;
                    else
                        decFMustQtyPack = Math.Ceiling(decMustQty / Convert.ToDecimal(dtSQL.Rows[i]["最小包装量"])) * Convert.ToDecimal(dtSQL.Rows[i]["最小包装量"]);


                    this.View.Model.SetValue("FCQty", decQTYALL, i);
                    this.View.Model.SetValue("FAllocatedQty", dtSQL.Rows[i]["已分配量"].ToString(), i);
                    this.View.Model.SetValue("FInventoryQty", dtSQL.Rows[i]["即时库存"].ToString(), i);
                    this.View.Model.SetValue("FMustQty", decMustQty, i);
                    this.View.Model.SetValue("FPackQty", dtSQL.Rows[i]["最小包装量"].ToString(), i);
                    this.View.Model.SetValue("FMustQtyPack", decFMustQtyPack, i);

                    decQTYALL = 0;
                }

                //刷新
                this.View.UpdateView("F_LZH_BackItemCounEntry");                
            }
        }
    }
}
