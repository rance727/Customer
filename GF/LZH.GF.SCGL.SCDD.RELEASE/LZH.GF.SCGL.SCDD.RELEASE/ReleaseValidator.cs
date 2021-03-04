using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.K3.MFG.PRD.App.Core;

using Kingdee.K3.MFG.PRD.App.Core.Validate;
using Kingdee.BOS.Core;
using System.Data;
using Kingdee.BOS.App.Data;
using Kingdee.BOS;
using Kingdee.BOS.Core.Bill;

namespace LZH.GF.SCGL.SCDD.RELEASE
{
    public class ReleaseValidator : AbstractMoBusinessStateValidator
    {
        public override string OperationName
        {

            get
            {
                return this.BizStateContext.IsExecuteForward
                        ? Kingdee.BOS.Resource.ResManager.LoadKDString("下达", "015075000002436", Kingdee.BOS.Resource.SubSystemType.MFG)
                        : Kingdee.BOS.Resource.ResManager.LoadKDString("反下达", "015075000002471", Kingdee.BOS.Resource.SubSystemType.MFG);
            }
        }

        protected override string[] PassedEntryState
        {
            get
            {
                return this.BizStateContext.IsExecuteForward
                    ? new string[]
                    {
                        "2"
                    }
                    : new string[]
                    {
                        "3","4","5","6","7"
                    };
            }
        }

        protected override void ValidateData()
        {
            base.ValidateData();

            if (this.IsExecuteForward)
            {
                CheckData();

                CheckAllSet();
            }

        }

        private void CheckData()
        {
            Context context = this.BizStateContext.Context;



            //for循环,读取数据
            DataTable dtMaterial = new DataTable();
            dtMaterial.Columns.Add("物料编码", typeof(string));
            dtMaterial.Columns.Add("总数", typeof(int));
            foreach (var data in this.BizStateContext.InputBizData)
            {
                DynamicObject dynamicMaterial = data["MaterialId"] as DynamicObject;
                string MaterialNumber = dynamicMaterial["Number"].ToString();
                bool bolisin = false;
                int introw = 0;
                for (int i = 0; i < dtMaterial.Rows.Count; i++)
                {
                    if (dtMaterial.Rows[i]["物料编码"].ToString() == MaterialNumber)
                    {
                        bolisin = true;
                        introw = i;
                    }
                }

                if (!bolisin)
                {
                    DataRow dr = dtMaterial.NewRow();

                    dr["物料编码"] = MaterialNumber;
                    dr["总数"] = 1;

                    dtMaterial.Rows.Add(dr);
                }
                else
                {
                    dtMaterial.Rows[introw]["总数"] = Convert.ToInt32(dtMaterial.Rows[introw]["总数"]) + 1;
                }

            }


            foreach (var data in this.BizStateContext.InputBizData)
            {

                string strMaterialNumber = (data["MaterialId"] as DynamicObject)["Number"].ToString();

                string strsql = @"select 1
                                        from T_PRD_MO t
                                        inner join T_PRD_MOENTRY t1 on t1.FID = t.FID
                                        inner join T_ORG_ORGANIZATIONS t2 on t2.FORGID = t.FPrdOrgId
                                        inner join T_BD_MATERIAL t3 on t3.FMATERIALID = t1.FMATERIALID
                                        inner join T_PRD_MOENTRY_A t4 on t4.FENTRYID = t1.FENTRYID  
                                        where t4.FSTATUS in ('3','4','5')
                                        and t3.FNUMBER = '" + strMaterialNumber + "'";
                DataTable dt = DBUtils.ExecuteDataSet(context, strsql).Tables[0];

                DataRow[] dr = dtMaterial.Select("物料编码= '" + strMaterialNumber + "'");


                if (dt.Rows.Count + Convert.ToInt32(dr[0]["总数"]) > 3)
                    this.AddValidationError(data,
                        string.Format("生产订单{0}第{1}行分录，物料{2}已存在三条或三条以上已下达，开工，完工状态的任务单，不允许下达"
                            , (data.Parent as DynamicObject)["BillNo"]
                            , data["Seq"]
                            , strMaterialNumber));
            }
        }

        private void CheckAllSet()
        {
            if (this.BizStateContext.InputBizData.Count > 0)
            {
                string strProORGNumber = (this.BizStateContext.InputBizData[0]["StockInOrgId"] as DynamicObject)["Number"].ToString();

                Context context = this.BizStateContext.Context;

                //即时库存
                string strInventory = @"select t3.FMATERIALID 物料内码, sum(t.FBASEQTY) 即时库存
	from T_STK_INVENTORY t
	inner join T_BD_STOCK t1 on t1.FSTOCKID = t.FSTOCKID and t1.FMOALLSET = 1
	inner join T_BD_MATERIAL t2 on t2.FMATERIALID = t.FMATERIALID
	inner join (select t.FMATERIALID,t.FNUMBER
				from T_BD_MATERIAL t
				inner join T_ORG_ORGANIZATIONS t1 on t1.FORGID = t.FUSEORGID
				where 1=1 and t1.FNUMBER = '" + strProORGNumber + @"'
				) t3 on t3.FNUMBER = t2.FNUMBER
	inner join T_ORG_ORGANIZATIONS t4 on t4.FORGID = t.FSTOCKORGID
	where t4.FNUMBER = '" + strProORGNumber + @"'
    group by t3.FMATERIALID";

                DataTable dtInventory = DBUtils.ExecuteDataSet(context, strInventory).Tables[0];

                foreach (var data in this.BizStateContext.InputBizData)
                {
                    //获取需求数量
                    string strSQLPPBOM = @"/*dialect*/
select t6.FNUMBER 物料编码, 
	t3.FMATERIALID 物料内码, 
	sum(cast(FMustQty  -  FSelPickedQty  +  FGoodReturnQty  +  FINCDefectReturnQty -  FReturnQty as float)) 需求数量,
	cast(isnull(t7.FQTY,0) as float) 已分配数量, cast(0 as float) 即时库存, cast(0 as float) 剩余需求数量
from T_PRD_MO t
inner join T_PRD_MOENTRY t1 on t1.FID = t.FID
inner join T_PRD_PPBOM t2 on t2.FMOENTRYID = t1.FENTRYID
inner join T_PRD_PPBOMENTRY t3 on t3.FID = t2.FID
inner join T_PRD_PPBOMENTRY_Q t4 on t4.FENTRYID = t3.FENTRYID
inner join T_BD_MATERIALPRODUCE t5 on t5.FMATERIALID = t3.FMATERIALID and t5.FISKITTING = 1
inner join T_BD_MATERIAL t6 on t6.FMATERIALID = t3.FMATERIALID
left join (select t7.FNUMBER, t3.FMATERIALID, SUM(FMustQty  -  FSelPickedQty  +  FGoodReturnQty  +  FINCDefectReturnQty -  FReturnQty)  FQTY
			from T_PRD_MO t
			inner join T_PRD_MOENTRY t1 on t1.FID = t.FID
			inner join T_PRD_PPBOM t2 on t2.FMOENTRYID = t1.FENTRYID
			inner join T_PRD_PPBOMENTRY t3 on t3.FID = t2.FID
			inner join T_PRD_PPBOMENTRY_Q t4 on t4.FENTRYID = t3.FENTRYID
			inner join T_BD_MATERIALPRODUCE t5 on t5.FMATERIALID = t3.FMATERIALID and t5.FISKITTING = 1
			inner join T_PRD_MOENTRY_A t6 on t6.FENTRYID = t1.FENTRYID and t6.FSTATUS in ('3','4','5')
			inner join T_BD_MATERIAL t7 on t7.FMATERIALID = t3.FMATERIALID
			where 1=1
			GROUP BY t7.FNUMBER, T3.FMATERIALID ) t7 on t7.FMATERIALID = t3.FMATERIALID
where 1=1
and t.FBILLNO = '" + (data.Parent as DynamicObject)["BillNo"].ToString() + @"' and t1.FSEQ = '" + data["Seq"].ToString() + "'" +
    "group by t3.FMATERIALID, t6.FNUMBER, isnull(t7.FQTY,0)";


                    DataTable dtSQLPPBOM = DBUtils.ExecuteDataSet(context, strSQLPPBOM).Tables[0];

                    //扣减即时库存

                    for (int x = 0; x < dtSQLPPBOM.Rows.Count; x++)
                    {
                        for (int k = 0; k < dtInventory.Rows.Count; k++)
                        {
                            if (dtInventory.Rows[k]["物料内码"].ToString() == dtSQLPPBOM.Rows[x]["物料内码"].ToString())
                            {
                                decimal decXQqty = Convert.ToDecimal(dtSQLPPBOM.Rows[x]["需求数量"]) + Convert.ToDecimal(dtSQLPPBOM.Rows[x]["已分配数量"]);
                                dtSQLPPBOM.Rows[x]["即时库存"] = Convert.ToDecimal(dtInventory.Rows[k]["即时库存"]);
                                dtSQLPPBOM.Rows[x]["剩余需求数量"] = decXQqty - Convert.ToDecimal(dtInventory.Rows[k]["即时库存"]);
                                dtInventory.Rows[k]["即时库存"] = Convert.ToDecimal(dtInventory.Rows[k]["即时库存"]) - decXQqty;
                            }
                        }
                    }

                    DataRow[] dataRows = dtSQLPPBOM.Select("剩余需求数量>0");
                    
                    if (dataRows.Length > 0)
                    {
                        string strMessage = "子项物料：【";
                        foreach (DataRow dr in dataRows)
                        {
                            strMessage += dr["物料编码"].ToString() + ",";
                        }

                        strMessage += "】，需求数量：【";
                        foreach (DataRow dr in dataRows)
                        {
                            strMessage += dr["需求数量"].ToString() + ",";
                        }

                        strMessage += "】+已分配数量：【";
                        foreach (DataRow dr in dataRows)
                        {
                            strMessage += dr["已分配数量"].ToString() + ",";
                        }

                        strMessage += "】-即时库存：【";
                        foreach (DataRow dr in dataRows)
                        {
                            strMessage += dr["即时库存"].ToString() + ",";
                        }

                        strMessage += "】>0";

                        this.AddValidationError(data,
                            string.Format("生产订单{0}第{1}行分录，{2}"
                                , (data.Parent as DynamicObject)["BillNo"]
                                , data["Seq"]
                                , strMessage));
                    }
                }
            }
        }
    }
}
