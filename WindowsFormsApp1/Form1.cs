using INI;
using KingViewClient;
using MaterialSkin;
using MaterialSkin.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
namespace HLHApp
{
    public partial class Form1 : MaterialForm
    {
        public Form1()
        {
            InitializeComponent();
            var materialSkinManager = MaterialSkinManager.Instance;
            materialSkinManager.AddFormToManage(this);
            materialSkinManager.Theme = MaterialSkinManager.Themes.LIGHT;
            materialSkinManager.ColorScheme = new ColorScheme(Primary.BlueGrey800, Primary.BlueGrey900, Primary.BlueGrey500, Accent.LightBlue200, TextShade.WHITE);
            recordTime = System.DateTime.MinValue;  // 改的地方
        }
        private static byte[] Keys = { 0x12, 0x34, 0x56, 0x78, 0x90, 0xAB, 0xCD, 0xEF };
        private static string KVKey = "kv@DAT_s";
        private static string KingviewUser = "无";
        private static string KingviewPass = "";
        private static LineForm newline;
        private static DateTime recordTime;
        private static bool ShowChart = false;
        /// <summary>
        /// 组态王服务器地址
        /// </summary>
        private static string KingviewHost = "127.0.0.1";
        private static string KingviewPort = "41195";
        /// <summary>
        /// 组态王Tcpip终端组件
        /// </summary>
        private static AxKvTcpipClientOcxLib.AxKvTcpipClientOcx KingViewClient;
        /// <summary>
        /// 是否登录
        /// </summary>
        private static bool LoginFlag = false;
        /// <summary>
        /// 是否保存变量
        /// </summary>
        private static bool SaveFlag = false;
        /// <summary>
        /// 站点数
        /// </summary>
        private static ushort stationsNum = 0;
        /// <summary>
        /// 站点列表
        /// </summary>
        private static Dictionary<ushort, string> StationList = new Dictionary<ushort, string>();
        /// <summary>
        /// 变量名列表
        /// </summary>
        private static Dictionary<string, string> VarNameList = new Dictionary<string, string>();
        /// <summary>
        /// 变量列表
        /// </summary>
        private static DataTable VarList = null;
        /// <summary>
        /// 订阅列表
        /// </summary>
        private static List<string> SubList = new List<string>();
        /// <summary>
        /// 保存列表
        /// </summary>
        private static List<string> SaveVarList = new List<string>();
        public System.Windows.Forms.Timer Timer { get; set; }
        
        private void Form1_Load(object sender, EventArgs e)
        {
            InitParam();
            InitKvClient();
            //ConnectDatabase();
        }
        private void CreateVarListTable()
        {
            VarList = new DataTable("varlist");
            VarList.Columns.Add("站点ID", typeof(ushort));
            VarList.Columns.Add("站点名", typeof(string));
            VarList.Columns.Add("变量ID", typeof(uint));
            VarList.Columns.Add("变量名", typeof(string));
            VarList.Columns.Add("变量类型", typeof(string));
            VarList.Columns.Add("变量值", typeof(string));
            VarList.Columns.Add("时间戳", typeof(DateTime));
            VarList.Columns.Add("质量戳", typeof(string));
            VarList.Columns.Add("订阅", typeof(string));
            VarList.Columns.Add("采集", typeof(string));
        }
        private void InitParam()
        {
            KingviewHost = GetKVHost();
            KingviewPort = GetKVPort();
            KingviewUser = GetKVUser();
            KingviewPass = Decrypt(GetKVPassword(), KVKey);
            //DB = new KingviewDB(DBConnstr);
        }
        private void InitKvClient()
        {
            if (KingViewClient == null)
            {
                KingViewClient = new AxKvTcpipClientOcxLib.AxKvTcpipClientOcx();
                Controls.Add(KingViewClient);
                ((System.ComponentModel.ISupportInitialize)(KingViewClient)).BeginInit();
                KingViewClient.Location = new System.Drawing.Point(0, 0);
                KingViewClient.Size = new System.Drawing.Size(0, 0);
                KingViewClient.Name = "KingViewClient";
                ((System.ComponentModel.ISupportInitialize)(KingViewClient)).EndInit();
                KingViewClient.Event_LoginServerOK += KvClient_Event_LoginServerOK;
                KingViewClient.Event_LogoutServerOk += KvClient_Event_LogoutServerOk;
                KingViewClient.Event_ServerClose += KvClient_Event_ServerClose;
                KingViewClient.Event_ServerDisconnect += KvClient_Event_ServerDisconnect;
                KingViewClient.Event_VariableValueChanged += KvClient_Event_VariableValueChanged;
                //KingViewClient.Event_VariableStampQualityChanged += KvClient_Event_VariableStampQualityChanged;
                //KingViewClient.Event_VariableStampTimeChanged += KvClient_Event_VariableStampTimeChanged;
                KingViewClient.Event_OcxMessage += KvClient_Event_OcxMessage;
            }
        }

        private DoWorkEventHandler GetBwLogin_DoWork()
        {
            return BwLogin_DoWork;
        }

        /// <summary>
        /// 登录服务器成功并取得客户号通知
        /// </summary>
        private void KvClient_Event_LoginServerOK(object sender, AxKvTcpipClientOcxLib._DKvTcpipClientOcxEvents_Event_LoginServerOKEvent e)
        {
            Console.WriteLine("login server ok!");
            LoginFlag = true;
            using (BackgroundWorker bwRefresh = new BackgroundWorker())
            {
                bwRefresh.DoWork += BwRefresh_DoWork;
                bwRefresh.RunWorkerCompleted += BwRefresh_RunWorkerCompleted;
                bwRefresh.RunWorkerAsync();
            }
        }
        /// <summary>
        /// 退出登录服务器成功通知
        /// </summary>

        private void KvClient_Event_LogoutServerOk(object sender, EventArgs e)
        {
            LoginFlag = false;
            CreateVarListTable();
            ShowVarList(VarList);
            //DB.UpdateKingViewConnStatus(KingviewHost, 0);
            MessageBox.Show("退出服务器", "组态王信息", MessageBoxButtons.OK);

            this.toolStripStatusLabelKingView.Text = "已退出";

        }

        /// <summary>
        /// 服务器关闭服务通知
        /// </summary>
        private void KvClient_Event_ServerClose(object sender, EventArgs e)
        {
            LoginFlag = false;
            CreateVarListTable();
            ShowVarList(VarList);

            //DB.UpdateKingViewConnStatus(KingviewHost, 0);

            MessageBox.Show("组态王服务器关闭", "组态王信息", MessageBoxButtons.OK);
            this.toolStripStatusLabelKingView.Text = "组态王服务器已关闭";
        }
        /// <summary>
        /// 与服务器失去连接通知
        /// </summary>
        private void KvClient_Event_ServerDisconnect(object sender, EventArgs e)
        {
            LoginFlag = false;
            CreateVarListTable();
            ShowVarList(VarList);

            //DB.UpdateKingViewConnStatus(KingviewHost, 0);
            MessageBox.Show("与组态王服务器失去连接", "组态王信息", MessageBoxButtons.OK);
            this.toolStripStatusLabelKingView.Text = "组态王服务器连接已断开";
        }
        /// <summary>
        /// 变量值改变通知(带时间戳和质量戳)
        /// </summary>
        private void KvClient_Event_VariableValueChanged(object sender, AxKvTcpipClientOcxLib._DKvTcpipClientOcxEvents_Event_VariableValueChangedEvent e)
        {
            if (VarList == null)
                return;
            ushort stationid = e.station_id;
            uint varid = e.variable_id;
            short vartype = e.variable_value_type;
            string varvalue = e.variable_value_string;
            long stamp_time_hi = ((long)e.stamp_time_hi) << 32;
            long stamp_time_lo = e.stamp_time_lo;
            ushort stamp_quality = e.stamp_quality;
            DateTime updatetime = DateTime.FromFileTime(stamp_time_hi + stamp_time_lo);
            for (int r = 0; r < VarList.Rows.Count; r++)
            {
                if (VarList.Rows[r][0].ToString() == stationid.ToString() && VarList.Rows[r][2].ToString() == varid.ToString())
                {
                    //System.Diagnostics.Debug.Write("id:" + stationid.ToString() + " value:" + varvalue + "\n");
                    VarList.Rows[r][5] = varvalue;
                    VarList.Rows[r][6] = updatetime;
                    VarList.Rows[r][7] = stamp_quality;
                   
                    System.Text.RegularExpressions.Regex reg = new System.Text.RegularExpressions.Regex(@"[0-9]+\.[0-9]+$");

                    if (reg.IsMatch(varvalue) && recordTime.Second != updatetime.Second && ShowChart)
                    {
                        Random re = new Random();
                        Console.WriteLine(varvalue + " " + updatetime + " " + stamp_quality);
                        double value = System.Convert.ToDouble(varvalue) + re.NextDouble();
                        newline.InsertMeasureModel(updatetime, value);
                        recordTime = updatetime;
                    }
                    //获得数值
                    /* if VarList.Rows[r][3].ToString().Equals("反应罐温度"))
                    varorid,varname,vartype,varvalue,qualitystamp,tistamp,stationid) value('" + varid + "','" + VarList.Rows[r][3].ToString() + "','" + vartype + "','" + varvalue + "','" + stamp_quality + "','" + updatetime + "','" + stationid + "')";
                    */
        }
    }
        }
        /// <summary>
        /// 变量质量戳改变通知(带时间戳)
        /// </summary>
        private void KvClient_Event_VariableStampQualityChanged(object sender, AxKvTcpipClientOcxLib._DKvTcpipClientOcxEvents_Event_VariableStampQualityChangedEvent e)
        {
        }

        /// <summary>
        /// 变量时间戳改变通知
        /// </summary>
        private void KvClient_Event_VariableStampTimeChanged(object sender, AxKvTcpipClientOcxLib._DKvTcpipClientOcxEvents_Event_VariableStampTimeChangedEvent e)
        {
        }

        /// <summary>
        /// 控件中其他（出错和调试信息）消息通知
        /// </summary>
        private void KvClient_Event_OcxMessage(object sender, AxKvTcpipClientOcxLib._DKvTcpipClientOcxEvents_Event_OcxMessageEvent e)
        {
            MessageBox.Show(e.message_buf, "组态王信息", MessageBoxButtons.OK);
        }

        /// <summary>
        /// 登录组态王的实时变量服务器
        /// </summary>
        /// <param name="server_ip_address">服务器（组态王）IP地址</param>
        /// <param name="server_port">服务器端口号</param>
        /// <param name="user_name">用户名</param>
        /// <param name="user_password">密码</param>
        /// <returns>
        /// 0：登录失败
        /// 1：登录通讯五秒钟内没有应答
        /// 2：登录成功
        /// 3：登录通讯五秒钟内有应答,但客户CliendId没有正确分配
        /// 4：已处在登录状态
        /// </returns>
        private short LoginServer(string server_ip_address, ushort server_port, string user_name, string user_password)
        {
            try
            {
                short flag = KingViewClient.Method_LoginServer(server_ip_address, server_port, user_name, user_password);
                return flag;
            }
            catch (Exception err)
            {
                return -1;
            }
        }

        /// <summary>
        /// 退出登录组态王的实时变量服务器
        /// </summary>
        /// <returns>
        /// 0:尚未登录
        /// 1:退出登录成功
        /// </returns>
        private short LogoutServer()
        {
            try
            {
                return KingViewClient.Method_LogoutServer();
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// 取得站点数
        /// </summary>
        /// <param name="station_number">站点数</param>
        /// <returns>
        /// 0：没有连接
        /// 1：通讯五秒钟内没有应答
        /// 2：成功
        /// </returns>
        private short GetStationNumber(ref ushort station_number)
        {
            try
            {
                return KingViewClient.Method_GetStationNumber(ref station_number);
            }
            catch (Exception err)
            {
                return -1;
            }
        }

        /// <summary>
        /// 取得站点名
        /// </summary>
        /// <param name="station_id">站点ID</param>
        /// <param name="station_name">站点名, 如果station_id相应的站点在服务端不存在则station_name="NULL"</param>
        /// <returns>
        /// 0：没有连接
        /// 1：通讯五秒钟内没有应答
        /// 2：成功
        /// </returns>
        private short GetStationName(ushort station_id, ref string station_name)
        {
            try
            {
                return KingViewClient.Method_GetStationName(station_id, ref station_name);
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>
        /// 取得某站点下变量数
        /// </summary>
        /// <param name="station_id">站点ID</param>
        /// <param name="variable_number">变量数</param>
        /// <returns>
        /// 0：没有连接
        /// 1：通讯五秒钟内没有应答
        /// 2：成功
        /// </returns>
        private short GetVariableNumber(ushort station_id, ref uint variable_number)
        {
            try
            {
                return KingViewClient.Method_GetVariableNumber(station_id, ref variable_number);
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>
        /// 取得某站点下某变量的变量名称
        /// </summary>
        /// <param name="station_id">站点ID</param>
        /// <param name="variable_id">变量ID</param>
        /// <param name="variable_name">变量名, 如果station_id,variable_id相应的变量在服务端不存在则variable_name="NULL"或=""</param>
        /// <returns>
        /// 0：没有连接
        /// 1：通讯五秒钟内没有应答
        /// 2：成功
        /// </returns>
        private short GetVariableName(ushort station_id, uint variable_id, ref string variable_name)
        {
            try
            {
                return KingViewClient.Method_GetVariableName(station_id, variable_id, ref variable_name);
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>
        /// 取得某站点下某变量的变量值(根据变量ID)
        /// </summary>
        /// <param name="station_id">站点ID</param>
        /// <param name="variable_id">变量ID</param>
        /// <param name="variable_value_type">变量类型</param>
        /// <param name="variable_value_string">变量值</param>
        /// <returns>
        /// 0：没有连接
        /// 1：通讯五秒钟内没有应答
        /// 2：成功
        /// 3：请求不存在变量的值
        /// </returns>
        private short GetVariableValueByVariableId(ushort station_id, uint variable_id, ref short variable_value_type, ref string variable_value_string)
        {
            try
            {
                return KingViewClient.Method_GetVariableValueByVariableId(station_id, variable_id, ref variable_value_type, ref variable_value_string);
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>
        /// 取得某站点下某变量的变量值(根据变量名)
        /// </summary>
        /// <param name="station_id">站点ID</param>
        /// <param name="variable_name">变量名</param>
        /// <param name="variable_value_type">变量类型</param>
        /// <param name="variable_value_string">变量值</param>
        /// <returns>
        /// 0：没有连接
        /// 1：通讯五秒钟内没有应答
        /// 2：成功
        /// 3：请求不存在变量的值
        /// </returns>
        private short GetVariableValueByVariableName(ushort station_id, string variable_name, ref short variable_value_type, ref string variable_value_string)
        {
            try
            {
                return KingViewClient.Method_GetVariableValueByVariableName(station_id, variable_name, ref variable_value_type, ref variable_value_string);
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>
        /// 取得某站点下某变量的带时间戳和质量戳的变量值(根据变量ID)
        /// </summary>
        /// <param name="station_id">站点ID</param>
        /// <param name="variable_id">变量ID</param>
        /// <param name="variable_value_type">变量类型</param>
        /// <param name="variable_value_string">变量值</param>
        /// <param name="stamp_time_hi">时间戳的高位</param>
        /// <param name="stamp_time_lo">时间戳的低位</param>
        /// <param name="stamp_quality">变量的质量戳</param>
        /// <returns>
        /// 0：没有连接
        /// 1：通讯五秒钟内没有应答
        /// 2：成功
        /// 3：请求不存在变量的值
        /// </returns>
        private short GetVariableValueWithStampByVariableId(ushort station_id, uint variable_id, ref short variable_value_type, ref string variable_value_string, ref uint stamp_time_hi, ref uint stamp_time_lo, ref ushort stamp_quality)
        {
            try
            {
                return KingViewClient.Method_GetVariableValueWithStampByVariableId(station_id, variable_id, ref variable_value_type, ref variable_value_string, ref stamp_time_hi, ref stamp_time_lo, ref stamp_quality);
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>
        /// 取得某站点下某变量的带时间和质量戳的变量值(根据变量名)
        /// </summary>
        /// <param name="station_id">站点ID</param>
        /// <param name="variable_name">变量名</param>
        /// <param name="variable_value_type">变量类型</param>
        /// <param name="variable_value_string">变量值</param>
        /// <param name="stamp_time_hi">时间戳的高位</param>
        /// <param name="stamp_time_lo">时间戳的低位</param>
        /// <param name="stamp_quality">变量的质量戳</param>
        /// <returns>
        /// 0：没有连接
        /// 1：通讯五秒钟内没有应答
        /// 2：成功
        /// 3：请求不存在变量的值
        /// </returns>
        private short GetVariableValueWithStampByVariableName(ushort station_id, string variable_name, ref short variable_value_type, ref string variable_value_string, ref uint stamp_time_hi, ref uint stamp_time_lo, ref ushort stamp_quality)
        {
            try
            {
                return KingViewClient.Method_GetVariableValueWithStampByVariableName(station_id, variable_name, ref variable_value_type, ref variable_value_string, ref stamp_time_hi, ref stamp_time_lo, ref stamp_quality);
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>
        /// 订阅某站点下某变量的当前值变化,质量戳变化，时间戳变化
        /// </summary>
        /// <param name="station_id">站点ID</param>
        /// <param name="variable_id">变量ID</param>
        /// <param name="subscibe_type">订阅类型
        /// 第0位决定是否订阅值的变化（1为订阅 0为不订阅）
        /// 第1位决定是否订阅质量戳的变化（1为订阅 0为不订阅）
        /// 第2位决定是否订阅时间戳的变化（1为订阅 0为不订阅）
        /// 第8位为1说明 订阅或取消订阅成功
        /// 第8位为0说明不成功：0x1000没有连接；0x2000通讯在五秒钟内没有应答；0x3000订阅不存在的变量。
        /// 值发生变化时会通过事件在VariableValueChangedKvtcpipclientocxctrl中通知
        /// 质量戳发生变化时会通过事件在VariableStampQualityChangedKvtcpipclientocxctrl中通知
        /// 时间戳发生变化时会通过事件在VariableStampTimeChangedKvtcpipclientocxctrl中通知</param>
        /// <returns></returns>
        private short SubscibeVariable(ushort station_id, uint variable_id, ushort subscibe_type)
        {
            try
            {
                return KingViewClient.Method_SubscibeVariable(station_id, variable_id, subscibe_type);
            }
            catch
            {
                return -1;
            }
        }
        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (LoginFlag)
                LogoutServer();
        }

        private void BwLogin_DoWork(object sender, DoWorkEventArgs e)
        {
            short result = 0;
            string strBack = "";

            result = LoginServer(KingviewHost, ushort.Parse(KingviewPort), KingviewUser, KingviewPass);

            switch (result)
            {
                case 0:
                    strBack = KingviewHost + "登录失败";
                    break;
                case 1:
                    strBack = KingviewHost + "登录通讯五秒钟内没有应答";
                    break;
                case 2:
                    strBack = KingviewHost + "登录成功";
                    //给登录标识符赋值
                    //CommonValues.IsLogin = true;
                    break;
                case 3:
                    strBack = KingviewHost + "登录通讯五秒钟内有应答,但客户CliendId没有正确分配";
                    break;
                case 4:
                    strBack = KingviewHost + "已处在登录状态";
                    break;
                default:
                    strBack = KingviewHost + "其他错误";
                    break;
            }
            e.Result = strBack;
        }

        private void BwLogin_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            MessageBox.Show(e.Result.ToString(), "组态王连接情况", MessageBoxButtons.OK);
            this.toolStripStatusLabelKingView.Text = e.Result.ToString();

        }
        //断开
        /*
        private void toolStripButtonLogout_Click(object sender, EventArgs e)
        {
            if (LoginFlag)
            {
                using (BackgroundWorker bwLogout = new BackgroundWorker())
                {
                    bwLogout.DoWork += BwLogout_DoWork;
                    bwLogout.RunWorkerCompleted += BwLogout_RunWorkerCompleted;
                    bwLogout.RunWorkerAsync();
                }
            }
        }
        */
            private void BwLogout_DoWork(object sender, DoWorkEventArgs e)
        {
            short result = 0;
            string strBack = "";
            result = LogoutServer();
            if (result == 0)
            { strBack = "未登录"; }
            else { strBack = "退出成功"; }
            e.Result = strBack;
        }

        private void BwLogout_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.toolStripStatusLabelKingView.Text = e.Result.ToString();
        }

        //刷新
        /*
         private void toolStripButtonRefreshVarList_Click(object sender, EventArgs e)
         {
             if (LoginFlag)
             {
                 using (BackgroundWorker bwRefresh = new BackgroundWorker())
                 {
                     bwRefresh.DoWork += BwRefresh_DoWork;
                     bwRefresh.RunWorkerCompleted += BwRefresh_RunWorkerCompleted;
                     bwRefresh.RunWorkerAsync();
                 }
             }
             else
             {
                 ShowMessage("请先登录服务器");
             }
         }
         */
            private void BwRefresh_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //MessageBox.Show(e.Result.ToString(), "组态王信息", MessageBoxButtons.OK);
        }

        private void BwRefresh_DoWork(object sender, DoWorkEventArgs e)
        {
            this.toolStripStatusLabelKingView.Text = "正在读取变量……";

            //获取站点数
            short result = GetStationNumber(ref stationsNum);
            if (stationsNum < 1)
            {
                e.Result = "获取站点失败";
                this.toolStripStatusLabelKingView.Text = "组态王获取站点失败";
                return;
            }
            //初始化数据库中的站点
            //DB.InitStationList();

            //初始化变量列表
            CreateVarListTable();
            //DB.InitVariableList();

            StationList = new Dictionary<ushort, string>(stationsNum);
            VarNameList = new Dictionary<string, string>();

            for (ushort stationid = 0; stationid < stationsNum; stationid++)
            {
                string stationsName = new string(' ', 50);
                result = GetStationName(0, ref stationsName);
                stationsName = stationsName.Replace('\0', ' ').Trim();
                StationList[stationid] = stationsName;              
                //保存站点列表
                uint varNum = 0;
                result = GetVariableNumber(stationid, ref varNum);
                for (uint varid = 0; varid < varNum; varid++)
                {
                    string varName = new string(' ', 50);
                    short varType = 0;
                    string varValue = new string(' ', 50);
                    uint varTimehi = 0;
                    uint varTimelo = 0;
                    ushort varQuality = 0;

                    //获取变量名称
                    GetVariableName(stationid, varid, ref varName);
                    varName = varName.Replace('\0', ' ').Trim();
                    VarNameList[string.Format("{0}|{1}", stationid, varid)] = varName;

                    //获取变量值
                    GetVariableValueWithStampByVariableId(stationid, varid, ref varType, ref varValue, ref varTimehi, ref varTimelo, ref varQuality);
                    varValue = varValue.Replace('\0', ' ').Trim();
                    DataRow row = VarList.NewRow();
                    row[0] = stationid;
                    row[1] = stationsName;
                    row[2] = varid;
                    row[3] = varName;
                    row[4] = getTypeName(varType);
                    row[5] = varValue;
                    long stamp_time_hi = ((long)varTimehi) << 32;
                    long stamp_time_lo = (long)varTimelo;
                    DateTime updatetime = DateTime.FromFileTime(stamp_time_hi + stamp_time_lo);
                    row[6] = updatetime;
                    row[7] = DBNull.Value;
                    //System.Diagnostics.Debug.Write("id:" + stationid.ToString() + " varValue:" + varValue + "\n");
                    if (!string.IsNullOrEmpty(varName))
                    {
                        if (varName.Substring(0, 1) != "$")
                        {
                            //订阅变量
                            AddSubList(string.Format("{0}|{1}", stationid, varid));
                            //DB.AddVariable(stationid, varid, varName, getTypeName(varType), varValue, updatetime);

                        }
                    }

                    if (SubList.BinarySearch(string.Format("{0}|{1}", stationid, varid)) < 0)
                    { row[8] = ""; }
                    else
                    {
                        SubscibeVariable(stationid, varid, 1);
                        row[8] = "是";
                    }
                    row[9] = DBNull.Value;
                    VarList.Rows.Add(row);
                }

            }
            ShowVarList(VarList);
            e.Result = "获取变量成功";
            this.toolStripStatusLabelKingView.Text = "获取变量成功";
            }

            private string getTypeName(short vartype)
        {
            string typename = "";
            switch (vartype)
            {
                case 3:
                    typename = "int";
                    break;
                case 4:
                    typename = "float";
                    break;
                case 16400:
                    typename = "string";
                    break;
                case 11:
                    typename = "bool";
                    break;
                default:
                    typename = vartype.ToString();
                    break;
            }
            return typename;
        }

        //订阅
        /*
        private void toolStripButtonSubscibeVar_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows == null || dataGridView1.SelectedRows.Count == 0)
                return;
            for (int r = 0; r < dataGridView1.SelectedRows.Count; r++)
            {
                int index = dataGridView1.SelectedRows[r].Index;
                ushort stationid = (ushort)dataGridView1.Rows[index].Cells[0].Value;
                uint varid = (uint)dataGridView1.Rows[index].Cells[2].Value;
                short result = SubscibeVariable(stationid, varid, 1);
                dataGridView1.Rows[index].Cells[8].Value = "是";
                AddSubList(string.Format("{0}|{1}", stationid, varid));
            }
            ShowMessage("订阅成功");
        }
        */
        private void AddSubList(string var)
        {
            int index = SubList.BinarySearch(var);
            if (index < 0)
            {
                SubList.Insert(~index, var);
            }
        }

        private void RemoveSubList(string var)
        {
            SubList.Remove(var);
        }

        private void AddSaveVarList(string var)
        {
            int index = SaveVarList.BinarySearch(var);
            if (index < 0)
            {
                SaveVarList.Insert(~index, var);
            }
        }

        private void RemoveSaveVarList(string var)
        {
            SaveVarList.Remove(var);
        }
        /*
        //采集
        private void toolStripButtonSaveVar_Click(object sender, EventArgs e)
        {
            DialogSave dlgSaveVar = new DialogSave();
            dlgSaveVar.StartPosition = FormStartPosition.CenterParent;
            DialogSave.StationList = StationList;
            DialogSave.VarNameList = VarNameList;
            DialogSave.SubList = SubList;
            DialogSave.SaveVarList = SaveVarList;
            dlgSaveVar.Initialize();

            if (dlgSaveVar.ShowDialog() == DialogResult.OK)
            {
                SaveVarList = DialogSave.SaveVarList;
                SaveFlag = true;
            }
        }
        */
        public delegate void SetDataTableCallback(DataTable table);

        private void ShowVarList(DataTable table)
        {
            if (table == null) return;
            if (this.InvokeRequired)
            {
                SetDataTableCallback d = new SetDataTableCallback(ShowVarList);
                this.Invoke(d, new object[] { table });
            }
            else
            {

                this.dataGridView1.DataSource = table;
                dataGridView1.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
                dataGridView1.Columns[2].AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
                dataGridView1.Columns[4].AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
                dataGridView1.Columns[6].DefaultCellStyle.Format = "yyyy-MM-dd HH:mm:ss";
                dataGridView1.Columns[7].AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
                dataGridView1.Columns[8].AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
            }
        }

        private void dataGridView1_RowStateChanged(object sender, DataGridViewRowStateChangedEventArgs e)
        {
            e.Row.HeaderCell.Value = string.Format("{0}", e.Row.Index + 1);
        }
        //退订
        /*
        private void toolStripButtonUnSubscibeVar_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows == null || dataGridView1.SelectedRows.Count == 0)
                return;
            for (int r = 0; r < dataGridView1.SelectedRows.Count; r++)
            {
                int index = dataGridView1.SelectedRows[r].Index;
                ushort stationid = (ushort)dataGridView1.Rows[index].Cells[0].Value;
                uint varid = (uint)dataGridView1.Rows[index].Cells[2].Value;
                short result = SubscibeVariable(stationid, varid, 0);
                dataGridView1.Rows[index].Cells[8].Value = "";
                RemoveSubList(string.Format("{0}|{1}", stationid, varid));
            }
            ShowMessage("退订成功");
        }
        
        //设置
        private void toolStripButtonOption_Click(object sender, EventArgs e)
        {
            DialogOption dlgOption = new DialogOption();
            dlgOption.KVHOST = KingviewHost;
            dlgOption.KVPORT = KingviewPort;
            dlgOption.KVUser = KingviewUser;
            dlgOption.KVPassword = KingviewPass;
            dlgOption.DBADDR = DBAddr;
            dlgOption.DBName = DBName;
            dlgOption.DBUser = DBUser;
            dlgOption.DBPassword = DBPass;
            dlgOption.InitControls();
            if (dlgOption.ShowDialog() == DialogResult.OK)
            {
                KingviewHost = dlgOption.KVHOST;
                SetKVHost(KingviewHost);
                KingviewPort = dlgOption.KVPORT;
                SetKVPort(KingviewPort);
                KingviewUser = dlgOption.KVUser;
                SetKVUser(KingviewUser);
                KingviewPass = dlgOption.KVPassword;
                SetKVPassword(Encrypt(KingviewPass, KVKey));
                DBAddr = dlgOption.DBADDR;
                SetDBHost(DBAddr);
                DBName = dlgOption.DBName;
                SetDBName(DBName);
                DBUser = dlgOption.DBUser;
                SetDBUser(DBUser);
                DBPass = dlgOption.DBPassword;
                SetDBPassword(Encrypt(DBPass, DBKey));
                DBConnstr = string.Format("server={0};database={1};uid={2};pwd={3}", DBAddr, DBName, DBUser, DBPass);
                //DB = new KingviewDB(DBConnstr);
            }
        }
        
        //关于
        private void toolStripButtonAbout_Click(object sender, EventArgs e)
        {
            AboutBox1 about = new AboutBox1();
            about.ShowDialog();
        }
        */

        static string GetKVHost()
        {
            return GetIniValue("KingView", "KVHost");
        }

        static void SetKVHost(string key)
        {
            SetIniValue("KingView", "KVHost", key);
        }
        static string GetKVPort()
        {
            return GetIniValue("KingView", "KVPort");
        }

        static void SetKVPort(string key)
        {
            SetIniValue("KingView", "KVPort", key);
        }

        static string GetKVUser()
        {
            return GetIniValue("KingView", "KVUser");
        }

        static void SetKVUser(string key)
        {
            SetIniValue("KingView", "KVUser", key);
        }

        static string GetKVPassword()
        {
            return GetIniValue("KingView", "KVPassword");
        }

        static void SetKVPassword(string key)
        {
            SetIniValue("KingView", "KVPassword", key);
        }

        static string GetDBHost()
        {
            return GetIniValue("Database", "DBHost");
        }

        static void SetDBHost(string key)
        {
            SetIniValue("Database", "DBHost", key);
        }

        static string GetDBName()
        {
            return GetIniValue("Database", "DBName");
        }

        static void SetDBName(string key)
        {
            SetIniValue("Database", "DBName", key);
        }

        static string GetDBUser()
        {
            return GetIniValue("Database", "DBUser");
        }

        static void SetDBUser(string key)
        {
            SetIniValue("Database", "DBUser", key);
        }

        static string GetDBPassword()
        {
            return GetIniValue("Database", "DBPassword");
        }

        static void SetDBPassword(string key)
        {
            SetIniValue("Database", "DBPassword", key);
        }

        static string GetIniValue(string section, string key)
        {
            IniFile ini = new IniFile(String.Format("{0}\\config.ini", Application.StartupPath));
            return ini.IniReadValue(section, key);
        }

        static void SetIniValue(string section, string key, string values)
        {
            IniFile ini = new IniFile(String.Format("{0}\\config.ini", Application.StartupPath));
            ini.IniWriteValue(section, key, values);
        }

        public static string Encrypt(string pToEncrypt, string sKey)
        {
            if (string.IsNullOrEmpty(pToEncrypt))
                return pToEncrypt;
            try
            {
                pToEncrypt = System.Web.HttpUtility.UrlEncode(pToEncrypt);
                DESCryptoServiceProvider des = new DESCryptoServiceProvider();
                byte[] inputByteArray = Encoding.GetEncoding("UTF-8").GetBytes(pToEncrypt);

                //建立加密对象的密钥和偏移量      
                //原文使用ASCIIEncoding.ASCII方法的GetBytes方法      
                //使得输入密码必须输入英文文本      
                des.Key = ASCIIEncoding.ASCII.GetBytes(sKey);
                des.IV = ASCIIEncoding.ASCII.GetBytes(sKey);
                MemoryStream ms = new MemoryStream();
                CryptoStream cs = new CryptoStream(ms, des.CreateEncryptor(), CryptoStreamMode.Write);

                cs.Write(inputByteArray, 0, inputByteArray.Length);
                cs.FlushFinalBlock();

                StringBuilder ret = new StringBuilder();
                foreach (byte b in ms.ToArray())
                {
                    ret.AppendFormat("{0:X2}", b);
                }
                ret.ToString();
                return ret.ToString();
            }
            catch { return pToEncrypt; }
        }


        public static string Decrypt(string pToDecrypt, string sKey)
        {
            if (string.IsNullOrEmpty(pToDecrypt))
                return pToDecrypt;
            try
            {
                DESCryptoServiceProvider des = new DESCryptoServiceProvider();

                byte[] inputByteArray = new byte[pToDecrypt.Length / 2];
                for (int x = 0; x < pToDecrypt.Length / 2; x++)
                {
                    int i = (Convert.ToInt32(pToDecrypt.Substring(x * 2, 2), 16));
                    inputByteArray[x] = (byte)i;
                }

                des.Key = ASCIIEncoding.ASCII.GetBytes(sKey);
                des.IV = ASCIIEncoding.ASCII.GetBytes(sKey);
                MemoryStream ms = new MemoryStream();
                CryptoStream cs = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Write);
                cs.Write(inputByteArray, 0, inputByteArray.Length);
                cs.FlushFinalBlock();

                StringBuilder ret = new StringBuilder();

                return System.Web.HttpUtility.UrlDecode(System.Text.Encoding.Default.GetString(ms.ToArray()));
            }
            catch { return pToDecrypt; }
        }

        private void Form1_FormClosed_1(object sender, FormClosedEventArgs e)
        {
            SingleInstance.DisposeRunFlag();
        }

        private void materialFlatButtonConnect_Click(object sender, EventArgs e)
        {
            if (!LoginFlag)
            {
                using (BackgroundWorker bwLogin = new BackgroundWorker())
                {
                    Console.WriteLine("start!");
                    bwLogin.DoWork += GetBwLogin_DoWork();
                    bwLogin.RunWorkerCompleted += BwLogin_RunWorkerCompleted;
                    bwLogin.RunWorkerAsync();
                }
            }
        }

        private void materialFlatButtonStop_Click(object sender, EventArgs e)
        {
            if (LoginFlag)
            {
                using (BackgroundWorker bwLogout = new BackgroundWorker())
                {
                    bwLogout.DoWork += BwLogout_DoWork;
                    bwLogout.RunWorkerCompleted += BwLogout_RunWorkerCompleted;
                    bwLogout.RunWorkerAsync();
                }
            }
        }

        private void materialRaisedButton2_Click(object sender, EventArgs e)
        {
            newline = new LineForm();
            newline.Show();
            ShowChart = true;
        }
    }
}
