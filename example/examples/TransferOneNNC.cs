using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThinNeo;

namespace example
{
    class TransferOneNNC : IExample
    {
        public string Name => "转账1nnc";

        public string ID => "tran nnc";

        public async Task Start()
        {
            string wif = "KwwJMvfFPcRx2HSgQRPviLv4wPrxRaLk7kfQntkH8kCXzTgAts8t";
            string targetAddress = "ALjSnMZidJqd18iQaoCgFun6iqWRm2cVtj";
            decimal sendCount = new decimal(1);

            byte[] prikey = ThinNeo.Helper.GetPrivateKeyFromWIF(wif);
            byte[] pubkey = ThinNeo.Helper.GetPublicKeyFromPrivateKey(prikey);
            string address = ThinNeo.Helper.GetAddressFromPublicKey(pubkey);

            byte[] script = null;
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                var array = new MyJson.JsonNode_Array();
                array.AddArrayValue("(addr)" + address);//from
                array.AddArrayValue("(addr)" + targetAddress);//to
                array.AddArrayValue("(int)"+"1"+"00");//value
                sb.EmitParamJson(array);//参数倒序入
                sb.EmitPushString("transfer");//参数倒序入
                sb.EmitAppCall(new Hash160("0xbab964febd82c9629cc583596975f51811f25f47"));
                script = sb.ToArray();
            }

            //获取自己的utxo
            Dictionary<string, List<UTXO>> dic_UTXO = await GetUTXOByAddress("https://api.nel.group/api/testnet", address);
            Transaction tran = makeTran(dic_UTXO,address,null, new ThinNeo.Hash256("0x602c79718b16e442de58778e148d0b1084e3b2dffd5de6b7b16cee7969282de7"), 0);
            tran.type = ThinNeo.TransactionType.InvocationTransaction;
            tran.version = 0;
            tran.attributes = new ThinNeo.Attribute[0];
            var idata = new ThinNeo.InvokeTransData();
            tran.extdata = idata;
            idata.script = script;
            idata.gas = 0;

            byte[] msg = tran.GetMessage();
            string msgstr = ThinNeo.Helper.Bytes2HexString(msg);
            byte[] signdata = ThinNeo.Helper.Sign(msg, prikey);
            tran.AddWitness(signdata, pubkey, address);
            string txid = tran.GetHash().ToString();
            byte[] data = tran.GetRawData();
            string rawdata = ThinNeo.Helper.Bytes2HexString(data);

            byte[] postdata;
            var url = HttpHelper.MakeRpcUrlPost("https://api.nel.group/api/testnet", "sendrawtransaction", out postdata, new MyJson.JsonNode_ValueString(rawdata));
            var result = await HttpHelper.HttpPost(url, postdata);
            MyJson.JsonNode_Object resJO = (MyJson.JsonNode_Object)MyJson.Parse(result);
            Console.WriteLine(resJO.ToString());
        }

        //获取地址的utxo来得出地址的资产  
        public static async Task<Dictionary<string, List<UTXO>>> GetUTXOByAddress(string api, string _addr)
        {
            MyJson.JsonNode_Object response = (MyJson.JsonNode_Object)MyJson.Parse(await HttpHelper.HttpGet(api + "?method=getutxo&id=1&params=['" + _addr + "']"));
            MyJson.JsonNode_Array resJA = (MyJson.JsonNode_Array)response["result"];
            Dictionary<string, List<UTXO>> _dir = new Dictionary<string, List<UTXO>>();
            foreach (MyJson.JsonNode_Object j in resJA)
            {
                UTXO utxo = new UTXO(j["addr"].ToString(), new ThinNeo.Hash256(j["txid"].ToString()), j["asset"].ToString(), decimal.Parse(j["value"].ToString()), int.Parse(j["n"].ToString()));
                if (_dir.ContainsKey(j["asset"].ToString()))
                {
                    _dir[j["asset"].ToString()].Add(utxo);
                }
                else
                {
                    List<UTXO> l = new List<UTXO>();
                    l.Add(utxo);
                    _dir[j["asset"].ToString()] = l;
                }
            }
            return _dir;
        }

        //拼交易体
        Transaction makeTran(Dictionary<string, List<UTXO>> dir_utxos, string fromAddress, string targetAddress, ThinNeo.Hash256 assetid, decimal sendcount)
        {
            if (!dir_utxos.ContainsKey(assetid.ToString()))
                throw new Exception("no enough money.");

            List<UTXO> utxos = dir_utxos[assetid.ToString()];

            Transaction tran = new Transaction();
            utxos.Sort((a, b) =>
            {
                if (a.value > b.value)
                    return 1;
                else if (a.value < b.value)
                    return -1;
                else
                    return 0;
            });

            decimal count = decimal.Zero;
            List<TransactionInput> list_inputs = new List<TransactionInput>();
            for (var i = 0; i < utxos.Count; i++)
            {
                TransactionInput input = new TransactionInput();
                input.hash = utxos[i].txid;
                input.index = (ushort)utxos[i].n;
                list_inputs.Add(input);
                count += utxos[i].value;
                if (count >= (sendcount))
                {
                    break;
                }
            }

            tran.inputs = list_inputs.ToArray();

            if (count >= sendcount)//输入大于等于输出
            {
                List<TransactionOutput> list_outputs = new List<TransactionOutput>();
                //输出
                if (sendcount > decimal.Zero)
                {
                    TransactionOutput output = new TransactionOutput();
                    output.assetId = assetid;
                    output.value = sendcount;
                    output.toAddress = ThinNeo.Helper.GetPublicKeyHashFromAddress(targetAddress);
                    list_outputs.Add(output);
                }

                //找零
                var change = count - sendcount;
                if (change > decimal.Zero)
                {
                    TransactionOutput outputchange = new TransactionOutput();
                    outputchange.toAddress = ThinNeo.Helper.GetPublicKeyHashFromAddress(fromAddress);
                    outputchange.value = change;
                    outputchange.assetId = assetid;
                    list_outputs.Add(outputchange);
                }
                tran.outputs = list_outputs.ToArray();
            }
            else
            {
                throw new Exception("no enough money.");
            }
            return tran;
        }

    }
}
