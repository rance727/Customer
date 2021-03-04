using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kingdee.K3.MFG.PRD.App.Core.Validate;
using Kingdee.K3.MFG.App.BizEngine.Validator;
using Kingdee.BOS.Util;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.K3.MFG.PRD.App.Core.MOBizState;

namespace LZH.GF.SCGL.SCDD.RELEASE
{
    public class ThreeAndAllSetRelease: Release
    {
        public override AbstractBusinessStateValidator[] GetValidator()
        {
            AbstractBusinessStateValidator[] Validatorarr = base.GetValidator();

            List<AbstractBusinessStateValidator> Validators = Validatorarr.IsEmpty() ?
                new List<AbstractBusinessStateValidator>() :
                Validatorarr.ToList();

            Validators.Add(new ReleaseValidator());

            return Validators.ToArray();
        }
    }
}
