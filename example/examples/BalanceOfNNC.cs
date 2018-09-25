using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThinNeo;

namespace example
{
    class BalanceOfNNC : IExample
    {
        public string Name => "获取 nnc 余额";

        public string ID => "balanceof";

        public async Task Start()
        {
            string address = "ALjSnMZidJqd18iQaoCgFun6iqWRm2cVtj";
            byte[] data = null;
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                MyJson.JsonNode_Array array = new MyJson.JsonNode_Array();
                array.AddArrayValue("(addr)" + address);//who
                sb.EmitParamJson(array);
                sb.EmitPushString("balanceOf");
                sb.EmitAppCall(new Hash160("0xbab964febd82c9629cc583596975f51811f25f47"));
                data = sb.ToArray();
            }
            string script = ThinNeo.Helper.Bytes2HexString(data);

            byte[] postdata;
            var url = HttpHelper.MakeRpcUrlPost("https://api.nel.group/api/testnet", "invokescript", out postdata, new MyJson.JsonNode_ValueString(script));
            var result = await HttpHelper.HttpPost(url, postdata);
            Console.WriteLine("得到的结果是：" + result);
        }
    }
}
