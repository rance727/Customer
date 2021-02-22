using Kingdee.BOS;
using Kingdee.BOS.Core;
//服务端
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
//校验器
using Kingdee.BOS.Core.Validation;
using System.ComponentModel;
using Kingdee.BOS.Orm.DataEntity;
using System.Data;
using Kingdee.BOS.App.Data;
using System;

namespace LZH.K3.SCZZ.SCGL.SCDD
{
    public class ThreeNotAllowed : AbstractOperationServicePlugIn
    {
        [Description("校验器")]
        [Kingdee.BOS.Util.HotUpdate]

        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);

            //采购员FPurchaserId
            e.FieldKeys.Add("FMaterialId");

            e.FieldKeys.Add("FPrdOrgId");

            e.FieldKeys.Add("");
        }

        //OnAddValidators操作执行前，加载操作校验器
        public override void OnAddValidators(AddValidatorsEventArgs e)
        {
            base.OnAddValidators(e);
            //订单一个物料三条
            TestValidator validator = new TestValidator();
            //是否需要校验,true需要
            validator.AlwaysValidate = true;
            //校验单据体FPOOrderEntry
            validator.EntityKey = "FTreeEntity";

            //齐套
            ValidatorAllready validatorAllready = new ValidatorAllready();
            validatorAllready.AlwaysValidate = true;
            validatorAllready.EntityKey = "FTreeEntity";

            //加载校验器
            e.Validators.Add(validator);
            e.Validators.Add(validatorAllready);
        }

        //三条记录
        //自定义校验器.派生:AbstractValidator
        private class TestValidator : AbstractValidator
        {
            //重写方法
            //数组ExtendedDataEntity,传递全部的信息
            public override void Validate(ExtendedDataEntity[] dataEntities, ValidateContext validateContext, Context ctx)
            {   //for循环,读取数据
                DataTable dtMaterial = new DataTable();
                dtMaterial.Columns.Add("物料编码", typeof(string));
                dtMaterial.Columns.Add("总数", typeof(int));
                foreach (ExtendedDataEntity obj in dataEntities)
                {
                    DynamicObject dynamicMaterial = obj["MaterialId"] as DynamicObject;
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

                foreach (ExtendedDataEntity obj in dataEntities)
                {
                    DynamicObject dynamicMaterial = obj["MaterialId"] as DynamicObject;
                    string MaterialNumber = dynamicMaterial["Number"].ToString();

                    //DynamicObject dynamicORG = obj["PrdOrgId"] as DynamicObject;
                    //string strORGNumber = dynamicORG["Number"].ToString();

                    DataRow[] rows = dtMaterial.Select("物料编码 = '" + MaterialNumber + "'");

                    string strsql = @"select 1
                                        from T_PRD_MO t
                                        inner join T_PRD_MOENTRY t1 on t1.FID = t.FID
                                        inner join T_ORG_ORGANIZATIONS t2 on t2.FORGID = t.FPrdOrgId
                                        inner join T_BD_MATERIAL t3 on t3.FMATERIALID = t1.FMATERIALID
                                        inner join T_PRD_MOENTRY_A t4 on t4.FENTRYID = t1.FENTRYID  
                                        where t4.FSTATUS in ('3','4','5')
                                        and t3.FNUMBER = '" + MaterialNumber + "'";
                    DataTable dt = DBUtils.ExecuteDataSet(this.Context, strsql).Tables[0];

                    //判断复选框是否勾选
                    if (dt.Rows.Count + Convert.ToInt32(rows[0]["总数"]) > 3)
                    {
                        validateContext.AddError(obj.DataEntity,
                            new ValidationErrorInfo
                            ("",//出错的字段Key，可以空
                            obj.DataEntity["Id"].ToString(),// 数据包内码，必填，后续操作会据此内码避开此数据包
                            obj.DataEntityIndex, // 出错的数据包在全部数据包中的顺序
                            obj.RowIndex,// 出错的数据行在全部数据行中的顺序，如果校验基于单据头，此为0
                            "001",//错误编码，可以任意设定一个字符，主要用于追查错误来源
                           "生产订单" + obj.BillNo + "第" + obj.RowIndex + 1 + "行" + "物料编码" + MaterialNumber + "，已存在三条或三条以上状态为下达、开工、完工状态的任务单，不允许下达。",// 错误的详细提示信息
                           "下达失败",// 错误的简明提示信息
                            Kingdee.BOS.Core.Validation.ErrorLevel.Error// 错误级别：警告、错误...
                            ));
                        //throw new KDBusinessException("", "FHHID导入重复数据！");
                    }
                }
            }
        }

        //齐套
        private class ValidatorAllready : AbstractValidator
        {
            //重写方法
            //数组ExtendedDataEntity,传递全部的信息
            public override void Validate(ExtendedDataEntity[] dataEntities, ValidateContext validateContext, Context ctx)
            {
                DataTable dtMaterial = new DataTable();
                dtMaterial.Columns.Add("物料编码", typeof(string));
                dtMaterial.Columns.Add("总数", typeof(int));
                foreach (ExtendedDataEntity obj in dataEntities)
                {
                    DynamicObject dynamicMaterial = obj["MaterialId"] as DynamicObject;
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
            }
        }
    }
}