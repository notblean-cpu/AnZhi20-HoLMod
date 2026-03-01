using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using System.Reflection;
using System.IO;

namespace MenKeConverter
{
    [BepInPlugin("cs.HoLMod.CustomGenerationMenKe.AnZhi20", "HoLMod.CustomGenerationMenKe", "2.0.0")]
    public class CustomGenerationMenKe : BaseUnityPlugin
    {
        // 语言管理内部类
        internal class LanguageManager
        {
            private static bool isChinese = true;
            private static Dictionary<string, string> translations = new Dictionary<string, string>()
            {
                // 窗口标题
                {"WindowTitle", "门客自定义生成器|Custom MenKe Generator"},
                
                // 状态信息
                {"Status", "状态: |Status: "},
                {"Ready", "准备就绪|Ready"},
                {"Generating", "正在生成门客数据...|Generating MenKe data..."},
                {"Adding", "正在添加到游戏中...|Adding to game..."},
                {"AddedSuccess", "成功添加 {0} 条门客数据到游戏中|Successfully added {0} MenKe data to the game"},
                {"GameDataNotLoaded", "游戏数据未加载，无法实时添加门客|Game data not loaded, cannot add MenKe in real-time"},
                
                // 错误信息
                {"ErrorCountRange", "错误: 生成数量必须在1到1000之间！|Error: Generation count must be between 1 and 1000!"},
                {"ErrorAgeRange", "错误: 门客年龄必须在18到60之间！|Error: MenKe age must be between 18 and 60!"},
                {"ErrorAddFailed", "添加失败: |Failed to add: "},
                
                // UI元素
                {"MenKeGender", "门客性别:|MenKe Gender:"},
                {"Female", "女|Female"},
                {"Male", "男|Male"},
                {"MenKeTalent", "门客天赋:|MenKe Talent:"},
                {"MenKeSkill", "门客技能:|MenKe Skill:"},
                {"MenKeAge", "门客年龄:|MenKe Age:"},
                {"GenerateCount", "生成数量:|Generate Count:"},
                
                // 按钮
                {"AddToGame", "实时添加到游戏|Add to Game Now"},
                
                // 使用说明
                {"Instructions", "使用说明:|Instructions:"},
                {"Instruction1", "1. 请在点击添加前先保存游戏，以便回档|1. Please save the game before clicking add to allow for rollback"},
                {"Instruction2", "2. 按F1键显示 / 隐藏菜单|2. Press F1 to show/hide the menu"},
                {"Instruction3", "3. 选择门客性别、天赋和技能|3. Select MenKe gender, talent, and skill"},
                {"Instruction4", "4. 选择门客年龄|4. Select MenKe age"},
                {"Instruction5", "5. 输入要生成的门客数量|5. Enter the number of MenKe to generate"},
                {"Instruction6", "6. 点击'实时添加到游戏'按钮直接添加到当前游戏中|6. Click 'Add to Game Now' button to add directly to the current game"},
                {"Instruction7", "7. 本mod作者：AnZhi20|7. Mod author: AnZhi20"},
                {"Instruction8", "8. 本mod版本：2.0.0|8. Mod version: 2.0.0"},
                
                // 天赋和技能
                {"Empty", "空|None"},
                {"TalentLiterary", "文|Literary"},
                {"TalentMartial", "武|Martial"},
                {"TalentBusiness", "商|Business"},
                {"TalentArt", "艺|Art"},
                {"SkillWitch", "巫|Witch"},
                {"SkillDoctor", "医|Doctor"},
                {"SkillPhysiognomy", "相|Physiognomy"},
                {"SkillDivination", "卜|Divination"},
                {"SkillCharm", "魅|Charm"},
                {"SkillCraftsman", "工|Craftsman"}
            };

            static LanguageManager()
            {
                // 检测系统语言
                try
                {
                    CultureInfo culture = CultureInfo.CurrentCulture;
                    isChinese = culture.Name.Contains("zh");
                }
                catch
                {
                    // 如果无法获取系统语言，默认使用中文
                    isChinese = true;
                }
            }

            public static string GetText(string key)
            {
                if (translations.TryGetValue(key, out string value))
                {
                    string[] parts = value.Split('|');
                    return isChinese && parts.Length > 0 ? parts[0] : (parts.Length > 1 ? parts[1] : key);
                }
                return key;
            }

            public static string GetText(string key, params object[] args)
            {
                string text = GetText(key);
                return string.Format(text, args);
            }
        }

        // 窗口及配置文件设置
        private ConfigEntry<float> menuWidth;
        private ConfigEntry<float> menuHeight;
        private ConfigEntry<string> menKeSalary; // 门客工资配置
        private ConfigEntry<string> MenKeMaxSkillPoints; // 门客超模技能点配置
        private static Rect windowRect;
        private static bool showMenu = false;
        private static bool blockGameInput = false;
        private static Vector2 scrollPosition;
        
        // 纹理对象
        private Texture2D backgroundTexture;
        private Texture2D buttonTextureA;
        private Texture2D buttonTextureB;
        private Texture2D frameTextureC;
        private Texture2D sureButtonTexture;
        private Texture2D searchTexture;
        
        // 定义常量
        const string A1 = ",";
        const string A2 = "|";
        const string A3 = "null";
        const string A4 = "M13"; 
        const string A5 = "0";
        const string A6 = "-1";
        const string Assignment = "100"; // 赋固定值
        

        // 定义变量
        private string Talent_Points = "0";    // 天赋点数 
        private string Skill_Points = "0";    // 技能点数 
        private string Alentin = "空"; // 天赋
        private string Skillsin = "空"; // 技能
        private int generateCount = 1;
        private string statusMessage = LanguageManager.GetText("Ready");
        private int customAge = 18;    // 年龄 
        
        // 当前插件版本
        private const string CURRENT_VERSION = "1.2.0"; // 与BepInPlugin属性中定义的版本保持一致


        // 定义数据列表
        static readonly List<string> After = Enumerable.Range(1, 19).Select(i => i.ToString()).ToList();  // 后发
        static readonly List<string> Body = Enumerable.Range(1, 29).Select(i => i.ToString()).ToList();   // 身体
        static readonly List<string> Face = Enumerable.Range(1, 2).Select(i => i.ToString()).ToList();    // 脸部
        static readonly List<string> Before = Enumerable.Range(1, 19).Select(i => i.ToString()).ToList(); // 前发
        static readonly List<string> BeforeRandom = Enumerable.Range(1, 9).Select(i => i.ToString()).ToList();  // 前随机
        static readonly List<string> Lifespan = new List<string> { "57", "58", "60", "62", "63", "64", "67", "68", "72", "73", "74", "75", "79", "80", "82" };  // 寿命
        static readonly List<string> Personality = Enumerable.Range(1, 6).Select(i => i.ToString()).ToList();  // 性格
        static readonly List<string> Remuneration = new List<string> { "200", "320", "460", "560", "680", "720", "760", "800", "920", "940", "1000", "1080", "1100", "1120", "1160", "1500", "1640", "2160", "2500" };  // 酬劳

        // 姓氏和名字列表
        static readonly List<string> Surname = new List<string> { "Tan", "Qian", "Wen", "Jin", "Gu", "Biang", "Zhang", "Song", "Bian", "Guo", "Xie", "Zhao", "Deng", "Su", "Fan", "Wei", "A", "Bai", "Jia", "Hao", "Cui", "Zhong", "Fang", "Yu", "Ding", "Kang", "Feng", "Be", "Beng", "Sun", "Shao", "Tian", "Ben", "Kong", "Biong", "Li", "Lin", "Meng", "Yang", "Ma", "Liang", "Gao", "Zhu", "Chang", "Lu", "Xu", "Duan", "Bia", "Dai", "Yin", "Xue", "Zheng", "Liao", "Ren", "Qiu", "Gong", "Liu", "Wan", "Bie", "Qin", "Lv", "Huang", "Shen", "Luo", "Mao", "Cai", "Wu", "Yi", "Xiao", "Wang", "Yao", "Pan", "Lei", "Biao", "Fu", "Hu", "Qiao", "Hou", "Cao", "Ye", "Peng", "Jiang", "Cheng", "Dong", "He", "Yuan", "Zhou", "Xiong", "Chen", "Tang", "Du", "Xia" };
        static readonly List<string> Namenan = new List<string> { "De", "Buai", "Le", "Fuan", "Jueng", "Kei", "Chie", "Tian", "Biou", "Fei", "Yang", "Chua", "Chong", "Chiao", "Gao", "Bong", "Bou", "Ceng", "Cua", "Bua", "Chin", "Buei", "Buang", "Bueng", "Cei", "Cen", "Ca", "An", "Hin", "Buan", "Chia", "Ya", "Hong", "Hei", "Chuen", "Ling", "Buo", "Zheng", "Hi", "Fiao", "Hai", "Fie", "Buen", "Qian", "Tai", "Xiu", "Chai", "Chan", "Cuen", "Fo", "Cing", "Huo", "Kiao", "Chuo", "Che", "Cha", "Kan", "Chian", "Liong", "Chiou", "Cia", "Cho", "Kang", "Ciao", "Chuai", "Chei", "Chuei", "Cuan", "Fueng", "Cie", "Din", "Fian", "Ching", "Diao", "Zhe", "Fi", "Ki", "Gian", "You", "Diou", "Lua", "Dia", "Duei", "Giang", "Chuan", "Kou", "Cian", "Kuen", "Wen", "Cu", "Co", "Ze", "Cuang", "Hiou", "Gi", "Ang", "Dueng", "Dao", "Shao", "Gin", "Fia", "Cou", "Hing", "Ciang", "Dei", "Cuo", "Zuo", "Cin", "Gang", "Ciou", "Ga", "Ciong", "Cuai", "Ging", "Guai", "Den", "Bin", "Fua", "Guo", "Jo", "Fiong", "Guen", "Diang", "Ha", "Xue", "Cueng", "Fuang", "Peng", "Do", "Guan", "Cuei", "Gei", "Pu", "Fao", "Fuai", "Fong", "Fuo", "Duen", "Hian", "Diong", "Giou", "Dou", "Chi", "Fuei", "Dua", "Gia", "Kiong", "Duai", "Zhong", "Xu", "Duang", "Fe", "Ran", "Fiou", "Fiang", "Rong", "Shuang", "Go", "Zhuan", "Tao", "Lan", "Can", "Fou", "Fai", "Jian", "Kueng", "Gai", "Hueng", "Guei", "Han", "Po", "Fin", "Geng", "Ja", "Fing", "Gen", "Fuen", "Rui", "Xian", "Hiao", "Ao", "Hiong", "Lei", "Hen", "Cheng", "Ye", "Jei", "Qi", "Ping", "Wu", "Jue", "Liang", "Kui", "Jang", "Xiong", "Gan", "Sa", "Juei", "Gie", "Pai", "Hiang", "Hia", "Bai", "Ning", "Jan", "Zhuang", "En", "Hua", "Lang", "Dang", "Yin", "Chang", "Min", "Yong", "Kai", "Yue", "Jai", "Liou", "Luan", "Hou", "Jiou", "Hie", "Ba", "Gou", "Chou", "Lou", "Giao", "Da", "Jia", "Chun", "Tong", "Ka", "Mu", "Giong", "Miao", "Gueng", "Zhuo", "Kuang", "Yan", "Shen", "Li", "Bing", "Ju", "Cun", "Kuai", "Liao", "Gua", "Zhen", "Jun", "Tu", "Fa", "Jao", "Ou", "Jua", "Qin", "Mei", "Zai", "Yun", "Huei", "Hao", "Xin", "Xiang", "King", "Kuan", "Juen", "Fu", "Lai", "Kin", "Jeng", "Guang", "Huen", "Ken", "Ho", "Teng", "Jong", "Jiong", "Luai", "Len", "Juai", "Lun", "Bei", "Shi", "Ge", "Zhao", "Kia", "Ying", "Nian", "Qiu", "Jen", "Xun", "Kua", "Je", "Jou", "Kuei", "Dian", "Lo", "Bao", "Mai", "Cong", "Chao", "Juang", "Juo", "Shan", "Qing", "Huan", "Hang", "Kiou", "Kian", "Kao", "Si", "Ming", "Xi", "Bang", "Yao", "Wei", "Song", "Kiang", "Feng", "Chu", "Luang", "Ji", "Keng", "Qiong", "Jie", "Zhou", "Ko", "Long", "Xuan", "Kie", "Zhi", "He", "Lie", "Huai", "Kong", "Ren", "Ku", "Lin", "Xing", "Jin", "Zhang", "Cai", "Nan", "Lu", "Lao", "Kun", "Mao", "Qie", "La", "Wang", "Hui", "Shu", "Heng", "Dong", "Jing", "Cang", "Sui", "Yu", "Kuo", "Lia", "Bi", "Run", "Yi", "Bo", "Zhan", "Sheng", "Shuo", "Qun", "Ri", "Rang", "Yuan", "Ce", "Zi", "Shou", "Mo", "Chen", "Ci" };
        static readonly List<string> Namenv = new List<string> { "Miou", "Nu", "Luen", "Mang", "Shei", "Xou", "Luei", "Muang", "Quai", "Pie", "Pe", "Lueng", "Ta", "Mueng", "Muei", "Mou", "Tua", "Lin", "Riang", "Dai", "Nua", "Ye", "Na", "Piou", "Me", "Muen", "Ban", "Muan", "Xe", "Muai", "Pueng", "Pi", "Sia", "Shong", "Ping", "Mia", "Sai", "Neng", "Mian", "Za", "Men", "No", "Er", "Zo", "Nei", "Zie", "Miang", "Nuen", "Ruang", "Puang", "Rai", "Nuai", "Mie", "Sei", "Ne", "Miong", "Mong", "Qan", "Zui", "Nen", "Ra", "Mua", "Zhan", "Puei", "Qu", "Niang", "Pong", "Puai", "Wian", "Niong", "Ni", "Muo", "Pua", "Niao", "Wiou", "Nia", "Sie", "Nie", "Reng", "Riong", "Zan", "Ruan", "Pao", "Qiang", "Shian", "Nai", "Ruei", "Nang", "Nin", "Nao", "Nuo", "Ro", "Nuei", "Niou", "Pang", "Run", "Zuai", "Bi", "Pa", "Shui", "Wang", "Piao", "Zi", "Sian", "Tuan", "Cong", "Chi", "Ma", "Pei", "Qong", "Quan", "Ziong", "Pen", "Rie", "Xei", "Rao", "Nueng", "Nou", "Zua", "Ren", "Zuen", "Qang", "Shuen", "Re", "Nuang", "Sua", "Leng", "Pan", "O", "Rian", "Qeng", "Zeng", "Qua", "Yai", "Quen", "Piang", "Ziou", "Seng", "Xong", "Yuang", "Piong", "Qei", "Siou", "Pia", "Yie", "Puan", "Pou", "Xun", "Pian", "Qe", "Gu", "Puen", "Riao", "Shing", "Xai", "Tei", "Luo", "Quo", "Qao", "Dian", "Yiong", "Xuo", "Qiong", "Sang", "Qai", "Riou", "Se", "Qa", "Puo", "Qiou", "Zhian", "Duo", "Rua", "Mi", "Xan", "Zang", "She", "Ria", "Xen", "Sou", "Suo", "Qen", "Quang", "Tuo", "Wao", "Quei", "Shia", "Qia", "Tie", "Zhuei", "Pin", "Qou", "Shie", "Bu", "Shuai", "Mo", "Wie", "Qo", "Shiou", "Sao", "Zhing", "Sin", "Yuai", "Wa", "San", "Rei", "Suei", "Queng", "Wia", "Siang", "Tin", "Wueng", "Zin", "Chu", "Bui", "Liu", "Hao", "Win", "Ring", "Suang", "Shai", "Duan", "Ruai", "Yiou", "Qian", "Sha", "Shuan", "Zhia", "Shen", "Suan", "Shiao", "Rin", "Ten", "Yei", "Wuang", "Tou", "Ruen", "Tuai", "Sen", "Jin", "Yong", "Suen", "Di", "Wuo", "Tiong", "Shu", "Suai", "Siong", "Wiang", "Sho", "Rueng", "Ya", "Tiao", "Yiao", "Siao", "Shin", "Lv", "Yang", "Wiao", "Xeng", "Zia", "Shua", "Ji", "So", "Dong", "Shang", "Tiou", "Shuei", "Xue", "Sing", "Meng", "Zen", "Tia", "Chiang", "Wua", "Tuen", "Ziang", "Dan", "Yue", "Weng", "Zuan", "Chun", "Xo", "Wong", "Xa", "Wuai", "Qiu", "Ran", "Xua", "Lei", "Zu", "Tiang", "Te", "Zei", "Xiao", "Wiong", "Sueng", "Xiu", "Su", "Mei", "Ti", "Zuang", "Tueng", "Die", "Bo", "Xuai", "Tuang", "Xuang", "Wou", "Juan", "To", "Ling", "Zhuai", "Fan", "Li", "Yuo", "Le", "Wo", "Wai", "Wuei", "Wuen", "Zhuo", "Cui", "Xuei", "Zhin", "Qiao", "Tuei", "Lian", "We", "Qin", "Ze", "Wing", "Rui", "Xao", "Wi", "Xang", "Xiou", "Zhu", "Yiang", "Fang", "Lu", "Xuen", "He", "Yao", "Xi", "Zhuen", "Wuan", "Han", "Wen", "Yuen", "Bun", "Bai", "Qing", "Yuei", "Nuan", "Shuang", "Wei", "Fen", "Ziao", "Yeng", "En", "Xing", "Yueng", "Shan", "Zhai", "Ruo", "Jie", "Ge", "Yia", "Mu", "Song", "Xueng", "Nan", "Yen", "Yo", "Peng", "An", "Huai", "Yua", "Zhei", "Ngo", "Zha", "Ning", "Xuan", "Zhiou", "Rou", "Yian", "Zuei", "Xu", "Miao", "Zho", "Min", "Zao", "Xian", "Chang", "Jiao", "Niu", "Zhua", "Zhiao", "Hui", "Zou", "Xiang", "Lang", "Fu", "Zueng", "Si", "Zhen", "Tong", "Bing", "Ting", "Wu", "Jue", "Zhie", "Chiong", "Bei", "Huan", "Qi", "Fei", "Chen", "Zhao", "Zing", "Yan", "Yi", "Ai", "Zian", "Xia", "Jun", "Kai", "Chuang", "Yuan", "Zong", "Ci", "Tian", "Jia", "Yun", "Rong", "Man", "Nong", "Hong", "Shi", "Ke", "Nian", "Cai", "Yin", "Feng", "Ming", "Ru", "Chui", "Tao", "Yu", "Wan", "Long", "Ying", "You", "Xin", "Lan", "Jing", "Zhi", "Hua" };
        // 天赋和技能列表
        static List<string> Alent = new List<string>();
        static List<string> Skills = new List<string>();

        // 初始化语言相关列表
        private void InitializeLanguageLists()
        {
            Alent.Clear();
            Alent.Add(LanguageManager.GetText("Empty"));
            Alent.Add(LanguageManager.GetText("TalentLiterary"));
            Alent.Add(LanguageManager.GetText("TalentMartial"));
            Alent.Add(LanguageManager.GetText("TalentBusiness"));
            Alent.Add(LanguageManager.GetText("TalentArt"));

            Skills.Clear();
            Skills.Add(LanguageManager.GetText("Empty"));
            Skills.Add(LanguageManager.GetText("SkillWitch"));
            Skills.Add(LanguageManager.GetText("SkillDoctor"));
            Skills.Add(LanguageManager.GetText("SkillPhysiognomy"));
            Skills.Add(LanguageManager.GetText("SkillDivination"));
            Skills.Add(LanguageManager.GetText("SkillCharm"));
            Skills.Add(LanguageManager.GetText("SkillCraftsman"));
        }

        
        // 界面控制变量
        private int selectedSex = 0; // 0: 女, 1: 男
        private int selectedAlent = 0; // 0: 空, 1: 文, 2: 武, 3: 商, 4: 艺
        private int selectedSkill = 0; // 0: 空, 1: 巫, 2: 医, 3: 相, 4: 卜, 5: 魅, 6: 工
        
        private void Awake()
        {
            Logger.LogInfo("门客自定义生成器已加载！");
            // 初始化语言相关列表
            InitializeLanguageLists();

            // DLL所在路径
            // DLL所在路径
            string assemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string dllDirectory = System.IO.Path.GetDirectoryName(assemblyPath);
            
            // 加载纹理
            backgroundTexture = LoadTexture(Path.Combine(dllDirectory, "Sprites", "Background.png"));
            buttonTextureA = LoadTexture(Path.Combine(dllDirectory, "Sprites", "BiaoQianA.png"));
            buttonTextureB = LoadTexture(Path.Combine(dllDirectory, "Sprites", "BiaoQianB.png"));
            frameTextureC = LoadTexture(Path.Combine(dllDirectory, "Sprites", "KuangC.png"));
            sureButtonTexture = LoadTexture(Path.Combine(dllDirectory, "Sprites", "SureButton.png"));
            searchTexture = LoadTexture(Path.Combine(dllDirectory, "Sprites", "Search.png"));
            
            // 版本检查配置
            var loadedVersion = Config.Bind("内部配置（Internal Settings）", "已加载版本（Loaded Version）", "", "用于跟踪插件版本，请勿手动修改").Value;
            
            // 检查是否需要更新配置
            bool isVersionUpdated = loadedVersion != CURRENT_VERSION;
            
            // 配置窗口大小
            menuWidth = Config.Bind<float>("窗口设置（Window Settings）", "窗口宽度（Menu Width）", 400f, "修改器主菜单宽度（Modifier main menu width）");
            menuHeight = Config.Bind<float>("窗口设置（Window Settings）", "窗口高度（Menu Height）", 700f, "修改器主菜单高度（Modifier main menu height）");
            
            // 配置门客工资
            menKeSalary = Config.Bind<string>("门客设置（MenKe Settings）", "门客月薪（MenKe Salary）", "50000", "门客的属性都这样了，月薪50k不过分吧？当然你也可以修改一下让门客当黑奴！（注意：门客的月薪不能超过100w，因为它不值这个价）（The attributes of the gatekeeper are all like this, isn't a monthly salary of 50k excessive? Of course, you can also modify it to make the servants black slaves! (Note: The monthly salary of a gatekeeper cannot exceed 100000 RMB, as it is not worth the price.)）");
            
            // 配置门客超模技能点
            MenKeMaxSkillPoints = Config.Bind<string>("门客设置（MenKe Settings）", "门客超模技能点（MenKe Max Skill Points）", "10", "门客属性点，默认值为10（MenKe attribute points, default value is 10）");
            
            // 如果版本更新，重新生成配置文件
            if (isVersionUpdated)
            {
                Logger.LogInfo($"检测到插件版本更新至 {CURRENT_VERSION}，正在重新生成配置文件...");
                
                try
                {
                    // 重置所有配置项
                    Config.Clear();
                    
                    // 重新绑定所有配置项
                    menuWidth = Config.Bind<float>("窗口设置（Window Settings）", "窗口宽度（Menu Width）", 400f, "修改器主菜单宽度（Modifier main menu width）");
                    menuHeight = Config.Bind<float>("窗口设置（Window Settings）", "窗口高度（Menu Height）", 700f, "修改器主菜单高度（Modifier main menu height）");
                    menKeSalary = Config.Bind<string>("门客设置（MenKe Settings）", "门客月薪（MenKe Salary）", "50000", "门客的属性都这样了，月薪50k不过分吧？当然你也可以修改一下让门客当黑奴！（注意：门客的月薪不能超过100w，因为它不值这个价）（The attributes of the gatekeeper are all like this, isn't a monthly salary of 50k excessive? Of course, you can also modify it to make the servants black slaves! (Note: The monthly salary of a gatekeeper cannot exceed 100000 RMB, as it is not worth the price.)）");
                    MenKeMaxSkillPoints = Config.Bind<string>("门客设置（MenKe Settings）", "门客超模技能点（MenKe Max Skill Points）", "10", "门客属性点，默认值为10");
                    
                    // 保存当前版本号
                    Config.Bind("内部配置（Internal Settings）", "已加载版本（Loaded Version）", CURRENT_VERSION, "用于跟踪插件版本，请勿手动修改");
                    
                    // 保存新的配置文件
                    Config.Save();
                    
                    Logger.LogInfo("配置文件已成功重新生成。");
                }
                catch (Exception ex)
                {
                    Logger.LogError("重新生成配置文件时出错: " + ex.Message);
                }
            }
            
            windowRect = new Rect(20f, 20f, menuWidth.Value, menuHeight.Value);
            
            // 应用Harmony补丁
            Harmony harmony = new Harmony("CustomGenerationMenKe");
            harmony.PatchAll();
        }
        
        private void Update()
        {
            // 按F1键切换菜单显示
            if (UnityEngine.Input.GetKeyDown(KeyCode.F1))
            {
                ToggleMenu();
            }
            
            // 按ESC键关闭菜单
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape) && showMenu)
            {
                CloseMenu();
            }
        }
        
        /// <summary>
        /// 切换菜单显示状态
        /// </summary>
        private void ToggleMenu()
        {
            showMenu = !showMenu;
            blockGameInput = showMenu;
        }
        
        /// <summary>
        /// 关闭菜单
        /// </summary>
        private void CloseMenu()
        {
            showMenu = false;
            blockGameInput = false;
        }
        
        private Texture2D LoadTexture(string relativePath)
        {            
            string pluginPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string texturePath = Path.Combine(pluginPath, relativePath);
            
            if (File.Exists(texturePath))
            {                
                try
                {                    
                    byte[] fileData = File.ReadAllBytes(texturePath);
                    Texture2D texture = new Texture2D(2, 2);
                    if (texture.LoadImage(fileData))
                    {                        
                        Logger.LogInfo($"Successfully loaded texture: {texturePath}");
                        return texture;
                    }                    
                    else                    
                    {                        
                        Logger.LogError($"Failed to load image from file: {texturePath}");
                    }                
                }                
                catch (Exception ex)                
                {                    
                    Logger.LogError($"Error loading texture {texturePath}: {ex.Message}");
                }            
            }
            else            
            {                
                Logger.LogWarning($"Texture file not found: {texturePath}");
            }
            return null;
        }
        
        private void OnGUI()
        {
            if (showMenu)
            {
                // 确保窗口位置和大小有效
                windowRect = new Rect(Mathf.Clamp(windowRect.x, 0, Screen.width - windowRect.width), 
                                     Mathf.Clamp(windowRect.y, 0, Screen.height - windowRect.height),
                                     windowRect.width, windowRect.height);
                
                // 绘制背景纹理，并添加边距
                if (backgroundTexture != null)
                {
                    // 计算带边距的绘制区域
                    Rect drawRect = new Rect(
                        windowRect.x - 50f,         // 左边距50f
                        windowRect.y - 40f,         // 上边距40f
                        windowRect.width + 100f,    // 左右各50f，总共100f
                        windowRect.height + 80f     // 上下各40f，总共80f
                    );
                    GUI.DrawTexture(drawRect, backgroundTexture, ScaleMode.StretchToFill);
                }
                else
                {
                    // 如果纹理加载失败，使用默认背景色作为备选
                    GUI.backgroundColor = new Color(0.9f, 0.9f, 0.9f, 0.95f);
                }
                
                // 创建无背景边框的窗口样式
                GUIStyle windowStyle = new GUIStyle(GUI.skin.window);
                windowStyle.normal.background = null; // 不使用默认背景
                
                // 使用自定义窗口样式创建窗口（只有边框）
                windowRect = UnityEngine.GUI.Window(0, windowRect, DrawWindow, "", windowStyle);
            }
        }
        
        private void DrawWindow(int windowID)
        {            
            // 保存原始样式
            GUIStyle originalWindowStyle = GUI.skin.window;
            GUIStyle originalBoxStyle = GUI.skin.box;
            GUIStyle originalButtonStyle = GUI.skin.button;
            
            // 允许窗口拖动
            GUI.DragWindow(new Rect(0, 0, windowRect.width, windowRect.height));
            
            // 标题
            GUIStyle titleStyle = new GUIStyle();
            titleStyle.fontSize = 18;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.alignment = TextAnchor.MiddleCenter;
            // 设置标题颜色为RGB:82,60,50（十六进制523C32）
            titleStyle.normal.textColor = new Color(82f/255f, 60f/255f, 50f/255f, 1f);
            UnityEngine.GUILayout.Label(LanguageManager.GetText("WindowTitle"), titleStyle, new UnityEngine.GUILayoutOption[] { UnityEngine.GUILayout.Height(50f) });

            UnityEngine.GUILayout.BeginVertical();
            UnityEngine.GUILayout.Space(10f);
            
            // 显示状态信息
            GUIStyle statusStyle = new GUIStyle(GUI.skin.box);
            if (frameTextureC != null)
            {                
                statusStyle.normal.background = frameTextureC;
            }
            // 设置状态文本颜色为RGB:82,60,50（十六进制523C32）
            statusStyle.normal.textColor = new Color(82f/255f, 60f/255f, 50f/255f, 1f);
            UnityEngine.GUILayout.Label(LanguageManager.GetText("Status") + statusMessage, statusStyle, new UnityEngine.GUILayoutOption[] { UnityEngine.GUILayout.Height(50f) });
            UnityEngine.GUILayout.Space(10f);
            
            // 创建通用标签样式，文本颜色为RGB:82,60,50
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.normal.textColor = new Color(82f/255f, 60f/255f, 50f/255f, 1f);
            
            // 性别选择
            UnityEngine.GUILayout.BeginHorizontal();
            UnityEngine.GUILayout.Label(LanguageManager.GetText("MenKeGender"), labelStyle, new UnityEngine.GUILayoutOption[] { UnityEngine.GUILayout.Width(80f) });
            
            // 分别为两个性别按钮创建样式
            GUIStyle femaleButtonStyle = new GUIStyle(GUI.skin.button);
            GUIStyle maleButtonStyle = new GUIStyle(GUI.skin.button);
            // 设置按钮文本颜色为RGB:82,60,50（十六进制523C32）
            femaleButtonStyle.normal.textColor = new Color(82f/255f, 60f/255f, 50f/255f, 1f);
            femaleButtonStyle.hover.textColor = new Color(82f/255f, 60f/255f, 50f/255f, 1f);
            maleButtonStyle.normal.textColor = new Color(82f/255f, 60f/255f, 50f/255f, 1f);
            maleButtonStyle.hover.textColor = new Color(82f/255f, 60f/255f, 50f/255f, 1f);
            
            // 应用纹理
            if (buttonTextureA != null)
            {
                // 默认使用buttonTextureA
                femaleButtonStyle.normal.background = buttonTextureA;
                femaleButtonStyle.hover.background = buttonTextureA;
                maleButtonStyle.normal.background = buttonTextureA;
                maleButtonStyle.hover.background = buttonTextureA;
            }
            
            if (buttonTextureB != null)
            {
                // 选中状态使用buttonTextureB
                if (selectedSex == 0) // 女
                {
                    femaleButtonStyle.normal.background = buttonTextureB;
                }
                else if (selectedSex == 1) // 男
                {
                    maleButtonStyle.normal.background = buttonTextureB;
                }
            }
            
            // 并排显示两个按钮
            if (UnityEngine.GUILayout.Button(LanguageManager.GetText("Female"), femaleButtonStyle, new UnityEngine.GUILayoutOption[] { UnityEngine.GUILayout.Width(80f) }))
            {
                selectedSex = 0;
            }
            
            if (UnityEngine.GUILayout.Button(LanguageManager.GetText("Male"), maleButtonStyle, new UnityEngine.GUILayoutOption[] { UnityEngine.GUILayout.Width(80f) }))
            {
                selectedSex = 1;
            }
            
            UnityEngine.GUILayout.EndHorizontal();
            UnityEngine.GUILayout.Space(10f);
            
            // 天赋选择
            UnityEngine.GUILayout.BeginHorizontal();
            UnityEngine.GUILayout.Label(LanguageManager.GetText("MenKeTalent"), labelStyle, new UnityEngine.GUILayoutOption[] { UnityEngine.GUILayout.Width(80f) });
            
            // 设置选择网格按钮样式
            GUIStyle buttonStyleA = new GUIStyle(GUI.skin.button);
            GUIStyle buttonStyleB = new GUIStyle(GUI.skin.button);
            if (buttonTextureA != null)
            {                
                buttonStyleA.normal.background = buttonTextureA;
                buttonStyleA.hover.background = buttonTextureA;
            }
            if (buttonTextureB != null)
            {                
                buttonStyleB.normal.background = buttonTextureB;
                buttonStyleB.active.background = buttonTextureB;
            }
            // 设置按钮文本颜色为RGB:82,60,50（十六进制523C32）
            buttonStyleA.normal.textColor = new Color(82f/255f, 60f/255f, 50f/255f, 1f);
            buttonStyleA.hover.textColor = new Color(82f/255f, 60f/255f, 50f/255f, 1f);
            buttonStyleB.normal.textColor = new Color(82f/255f, 60f/255f, 50f/255f, 1f);
            buttonStyleB.active.textColor = new Color(82f/255f, 60f/255f, 50f/255f, 1f);
            
            // 自定义选择网格绘制
            string[] talentOptions = Alent.ToArray();
            UnityEngine.GUILayout.BeginVertical();
            UnityEngine.GUILayout.BeginHorizontal();
            for (int i = 0; i < talentOptions.Length; i++)
            {                
                if (i > 0 && i % 3 == 0)
                {                    
                    UnityEngine.GUILayout.EndHorizontal();
                    UnityEngine.GUILayout.BeginHorizontal();
                }
                
                GUIStyle currentStyle = (i == selectedAlent) ? buttonStyleB : buttonStyleA;
                if (UnityEngine.GUILayout.Button(talentOptions[i], currentStyle, new UnityEngine.GUILayoutOption[] { UnityEngine.GUILayout.Width(80f) }))
                {                    
                    selectedAlent = i;
                }
            }
            UnityEngine.GUILayout.EndHorizontal();
            UnityEngine.GUILayout.EndVertical();
            
            UnityEngine.GUILayout.EndHorizontal();
            UnityEngine.GUILayout.Space(10f);
            
            // 技能选择
            UnityEngine.GUILayout.BeginHorizontal();
            UnityEngine.GUILayout.Label(LanguageManager.GetText("MenKeSkill"), labelStyle, new UnityEngine.GUILayoutOption[] { UnityEngine.GUILayout.Width(80f) });
            
            // 自定义技能选择网格绘制
            string[] skillOptions = Skills.ToArray();
            UnityEngine.GUILayout.BeginVertical();
            UnityEngine.GUILayout.BeginHorizontal();
            for (int i = 0; i < skillOptions.Length; i++)
            {                
                if (i > 0 && i % 3 == 0)
                {                    
                    UnityEngine.GUILayout.EndHorizontal();
                    UnityEngine.GUILayout.BeginHorizontal();
                }
                
                GUIStyle currentStyle = (i == selectedSkill) ? buttonStyleB : buttonStyleA;
                if (UnityEngine.GUILayout.Button(skillOptions[i], currentStyle, new UnityEngine.GUILayoutOption[] { UnityEngine.GUILayout.Width(80f) }))
                {                    
                    selectedSkill = i;
                }
            }
            UnityEngine.GUILayout.EndHorizontal();
            UnityEngine.GUILayout.EndVertical();
            
            UnityEngine.GUILayout.EndHorizontal();
            UnityEngine.GUILayout.Space(10f);
            UnityEngine.GUILayout.BeginHorizontal();
            UnityEngine.GUILayout.Label(LanguageManager.GetText("MenKeAge"), labelStyle, new UnityEngine.GUILayoutOption[] { UnityEngine.GUILayout.Width(80f) });
            customAge = (int)UnityEngine.GUILayout.HorizontalSlider(customAge, 18, 60, new UnityEngine.GUILayoutOption[] { UnityEngine.GUILayout.Width(200f) });
            UnityEngine.GUILayout.Label(customAge.ToString(), labelStyle, new UnityEngine.GUILayoutOption[] { UnityEngine.GUILayout.Width(40f) });
            UnityEngine.GUILayout.EndHorizontal();
            UnityEngine.GUILayout.Space(10f);
            
            // 数量输入
            UnityEngine.GUILayout.BeginHorizontal();
            UnityEngine.GUILayout.Label(LanguageManager.GetText("GenerateCount"), labelStyle, new UnityEngine.GUILayoutOption[] { UnityEngine.GUILayout.Width(80f) });
            
            // 创建带Search.png背景的输入框样式
            GUIStyle searchFieldStyle = new GUIStyle(GUI.skin.textField);
            // 设置文本居中对齐
            searchFieldStyle.alignment = TextAnchor.MiddleCenter;
            if (searchTexture != null)
            {                
                searchFieldStyle.normal.background = searchTexture;
                searchFieldStyle.normal.textColor = new Color(82f/255f, 60f/255f, 50f/255f, 1f);
                searchFieldStyle.focused.textColor = new Color(82f/255f, 60f/255f, 50f/255f, 1f);
                searchFieldStyle.focused.background = searchTexture;
            }
            else
            {                
                // 如果纹理加载失败，使用与图片完全相同的颜色
                searchFieldStyle.normal.background = null;
                searchFieldStyle.normal.textColor = new Color(82f/255f, 60f/255f, 50f/255f, 1f);
            }
            
            string countInput = UnityEngine.GUILayout.TextField(generateCount.ToString(), searchFieldStyle, new UnityEngine.GUILayoutOption[] { UnityEngine.GUILayout.Width(100f) });
            if (int.TryParse(countInput, out int count))
            {
                generateCount = UnityEngine.Mathf.Clamp(count, 1, 1000);
            }
            UnityEngine.GUILayout.EndHorizontal();
            UnityEngine.GUILayout.Space(20f);
            

            // 实时添加按钮
            GUIStyle sureButtonStyle = new GUIStyle(GUI.skin.button);
            if (sureButtonTexture != null)
            {                
                sureButtonStyle.normal.background = sureButtonTexture;
                sureButtonStyle.hover.background = sureButtonTexture;
                sureButtonStyle.active.background = sureButtonTexture;
            }
            if (UnityEngine.GUILayout.Button(LanguageManager.GetText("AddToGame"), sureButtonStyle, new UnityEngine.GUILayoutOption[] { UnityEngine.GUILayout.Height(40f) }))
            {
                if (generateCount < 1 || generateCount > 1000)
                {
                    statusMessage = LanguageManager.GetText("ErrorCountRange");
                    Logger.LogWarning("生成数量超出范围: " + generateCount);
                }
                else if (customAge < 18 || customAge > 60)
                {
                    statusMessage = LanguageManager.GetText("ErrorAgeRange");
                    Logger.LogWarning("门客年龄超出范围: " + customAge);
                }
                else
                {
                    RealTimeAddMenKeData();
                }
            }
            UnityEngine.GUILayout.Space(20f);
            
            // 使用说明 - 设置背景为KuangC.png
            if (frameTextureC != null)
            {                
                // 创建自定义样式用于使用说明部分
                GUIStyle instructionsStyle = new GUIStyle(GUI.skin.box);
                instructionsStyle.normal.background = frameTextureC;
                // 设置说明标题文本颜色为RGB:82,60,50（十六进制523C32）
                instructionsStyle.normal.textColor = new Color(82f/255f, 60f/255f, 50f/255f, 1f);
                
                UnityEngine.GUILayout.Label(LanguageManager.GetText("Instructions"), instructionsStyle);
                
                // 为滚动视图创建自定义样式
                GUIStyle scrollViewStyle = new GUIStyle();
                scrollViewStyle.normal.background = frameTextureC;
                scrollViewStyle.border = new RectOffset(10, 10, 10, 10);
                // 设置滚动视图文本颜色为RGB:82,60,50（十六进制523C32）
                scrollViewStyle.normal.textColor = new Color(82f/255f, 60f/255f, 50f/255f, 1f);
                
                scrollPosition = UnityEngine.GUILayout.BeginScrollView(scrollPosition, scrollViewStyle, new UnityEngine.GUILayoutOption[] { UnityEngine.GUILayout.Height(210f) });
            }
            else
            {                
                UnityEngine.GUILayout.Label(LanguageManager.GetText("Instructions"), UnityEngine.GUI.skin.box);
                scrollPosition = UnityEngine.GUILayout.BeginScrollView(scrollPosition, new UnityEngine.GUILayoutOption[] { UnityEngine.GUILayout.Height(210f) });
            }
            UnityEngine.GUILayout.Label(LanguageManager.GetText("Instruction1"), labelStyle);
            UnityEngine.GUILayout.Label(LanguageManager.GetText("Instruction2"), labelStyle); 
            UnityEngine.GUILayout.Label(LanguageManager.GetText("Instruction3"), labelStyle);
            UnityEngine.GUILayout.Label(LanguageManager.GetText("Instruction4"), labelStyle);
            UnityEngine.GUILayout.Label(LanguageManager.GetText("Instruction5"), labelStyle);
            UnityEngine.GUILayout.Label(LanguageManager.GetText("Instruction6"), labelStyle);
            UnityEngine.GUILayout.Label(LanguageManager.GetText("Instruction7"), labelStyle); 
            UnityEngine.GUILayout.Label(LanguageManager.GetText("Instruction8"), labelStyle);
            UnityEngine.GUILayout.EndScrollView();
            
            UnityEngine.GUILayout.EndVertical();
            
            // 恢复原始样式
            GUI.skin.window = originalWindowStyle;
            GUI.skin.box = originalBoxStyle;
            GUI.skin.button = originalButtonStyle;
        }
        

        
        /// <summary>
        /// 实时添加门客数据到游戏中（不修改GameData.es3）
        /// </summary>
        private void RealTimeAddMenKeData()
        {
            try
            {
                statusMessage = LanguageManager.GetText("Generating");
                Logger.LogInfo("开始实时添加门客数据");
                
                List<string> newMenKeData = new List<string>();
                
                for (int i = 0; i < generateCount; i++)
                {
                    // 生成随机ID
                    string Mrandom = (UnityEngine.Random.Range(1000000, 9999999) + DateTime.Now.Ticks).ToString();
                    Logger.LogInfo($"生成门客 {i+1}/{generateCount}，随机ID: {Mrandom}");
                    
                    // 随机选择姓氏、名字前缀和后缀
                    string RanSurname = Surname[UnityEngine.Random.Range(0, Surname.Count)];
                    string RanBefore = Before[UnityEngine.Random.Range(0, Before.Count)];
                    string RanAfter = After[UnityEngine.Random.Range(0, After.Count)];
                    string RanBody = Body[UnityEngine.Random.Range(0, Body.Count)];
                    string RanFace = Face[UnityEngine.Random.Range(0, Face.Count)];
                    string RanBeforeRandom = BeforeRandom[UnityEngine.Random.Range(0, BeforeRandom.Count)];
                    
                    // 生成年龄
                    int age = UnityEngine.Random.Range(16, 50);
                    string Age = age.ToString();
                    
                    // 随机选择性格和寿命
                    string RanPersonality = Personality[UnityEngine.Random.Range(0, Personality.Count)];
                    string RanLifespan = Lifespan[UnityEngine.Random.Range(0, Lifespan.Count)];
                    
                    // 薪酬设置并确保其有效
                    string RanRemuneration = "50000"; // 默认值
                    if (int.TryParse(menKeSalary.Value, out int salaryValue))
                    {
                        if (salaryValue >= 0 && salaryValue <= 1000000)
                        {
                            RanRemuneration = salaryValue.ToString();
                        }
                    }

                    if (int.TryParse(menKeSalary.Value, out int salaryValueCheck))
                        {
                            if (salaryValueCheck < 0)
                            {
                                RanRemuneration = "50000";
                            }
                            else if (salaryValueCheck > 1000000)
                            {
                                RanRemuneration = "50000";
                            }
                            else
                            {
                                RanRemuneration = salaryValueCheck.ToString();
                            }
                        }
                        
                    
                    // 根据性别选择名字
                    string RanName1, RanName2;
                    if (selectedSex == 1)  // 男
                    {
                        RanName1 = Namenan[UnityEngine.Random.Range(0, Namenan.Count)];
                        RanName2 = Namenan[UnityEngine.Random.Range(0, Namenan.Count)];
                    }
                    else  // 女
                    {
                        RanName1 = Namenv[UnityEngine.Random.Range(0, Namenv.Count)];
                        RanName2 = Namenv[UnityEngine.Random.Range(0, Namenv.Count)];
                    }


                    // 将选择的天赋赋值给对应变量
                    switch (Alent[selectedAlent])
                    {
                        case "空":
                            Alentin = "0";
                            Talent_Points = "0";
                            break;
                        case "文":
                            Alentin = "1";
                            Talent_Points = "100";
                            break;
                        case "武":
                            Alentin = "2";
                            Talent_Points = "100";
                            break;
                        case "商":
                            Alentin = "3";
                            Talent_Points = "100";
                            break;
                        case "艺":
                            Alentin = "4";
                            Talent_Points = "100";
                            break;
                        default:
                            Alentin = "0";
                            Talent_Points = "0";
                            break;
                    }


                    // 将选择的技能赋值给对应变量
                    switch (Skills[selectedSkill])
                    {
                        case "空":
                            Skillsin = "0";
                            Skill_Points = "0";
                            break;
                        case "巫":
                            Skillsin = "1";
                            Skill_Points = MenKeMaxSkillPoints.Value;
                            break;
                        case "医":
                            Skillsin = "2";
                            Skill_Points = MenKeMaxSkillPoints.Value;
                            break;
                        case "相":
                            Skillsin = "3";
                            Skill_Points = MenKeMaxSkillPoints.Value;
                            break;
                        case "卜":
                            Skillsin = "4";
                            Skill_Points = MenKeMaxSkillPoints.Value;
                            break;
                        case "魅":
                            Skillsin = "5";
                            Skill_Points = MenKeMaxSkillPoints.Value;
                            break;
                        case "工":
                            Skillsin = "6";
                            Skill_Points = MenKeMaxSkillPoints.Value;
                            break;
                        default:
                            Skillsin = "0";
                            Skill_Points = "0";
                            break;
                    }
                    

                    // 构建门客角色数据字符串
                    // 角色ID
                    string MenKe_ID = $"{A4}{Mrandom}";
                    // 角色合并 后发|身体|脸部|前发
                    string MenKe_JueSe = $"{RanAfter}{A2}{RanBody}{A2}{RanFace}{A2}{RanBefore}";
                    // 属性合并 姓名合并_姓辈字|前随机|天赋|天赋点数|性别|寿命|技能|幸运|性格|null
                    string MenKe_ShuXing = $"{RanSurname}{RanName1}{RanName2}{A2}{RanBeforeRandom}{A2}{Alentin}{A2}{Talent_Points}{A2}{selectedSex}{A2}{RanLifespan}{A2}{Skillsin}{A2}{Assignment}{A2}{RanPersonality}{A2}{A3}";
                    // 年龄
                    string MenKe_Age = customAge.ToString();
                    // 常规属性"文","武","商","艺","心情","声誉","魅力","健康","计谋","健康","体力"
                    string MenKe_ChangGui = $"{Assignment}";
                    // 住房属性
                    string MenKe_ZhuFang = $"{A5}{A2}{A3}{A2}{A3}";
                    // 位置属性 0
                    string MenKe_unknown = $"{A5}";


                    // 将门客数据直接添加到游戏的内存数据中
                    Logger.LogInfo($"检查Mainload.MenKe_Now是否为空: {(Mainload.MenKe_Now == null ? "是" : "否")}");
                    if (Mainload.MenKe_Now != null)
                    {
                        // 检查Mainload.HanMenMemberData_Click是否为空，如果为空则使用默认值
                        Logger.LogInfo($"检查Mainload.HanMenMemberData_Click是否为空: {(Mainload.HanMenMemberData_Click == null ? "是" : "否")}");
                        string hanMenData18 = "0";
                        if (Mainload.HanMenMemberData_Click != null)
                        {
                            Logger.LogInfo($"Mainload.HanMenMemberData_Click.Count = {Mainload.HanMenMemberData_Click.Count}");
                            if (Mainload.HanMenMemberData_Click.Count > 18)
                            {
                                hanMenData18 = Mainload.HanMenMemberData_Click[18];
                                Logger.LogInfo($"获取HanMenMemberData_Click[18] = {hanMenData18}");
                            }
                        }

                        
                        Mainload.MenKe_Now.Add(new List<string>
                        {
                            MenKe_ID,
                            MenKe_JueSe,
                            MenKe_ShuXing,
                            MenKe_Age,
                            MenKe_ChangGui,//文
                            MenKe_ChangGui,//武
                            MenKe_ChangGui,//商
                            MenKe_ChangGui,//艺
                            MenKe_ChangGui,//心情
                            "0|null|null",
                            "0",//状态
                            MenKe_ChangGui,//声誉
                            hanMenData18,
                            MenKe_ChangGui,//魅力
                            MenKe_ChangGui,//健康
                            MenKe_ChangGui,//计谋
                            Skill_Points,//技能点
                            "-1",//怀孕月份
                            RanRemuneration,//工资，根据配置文件判断
                            MenKe_ChangGui,//体力
                            "0",
                            "null"//特殊标签，Mainload.HanMenMemberData_Click[26]
                        });
                        Logger.LogInfo("成功添加门客到游戏中：" + MenKe_JueSe);
                    }
                }

                statusMessage = LanguageManager.GetText("Adding");
                
                if (Mainload.MenKe_Now != null)
                {
                    statusMessage = LanguageManager.GetText("AddedSuccess", generateCount);
                }
                else
                {
                    statusMessage = LanguageManager.GetText("GameDataNotLoaded");
                    Logger.LogWarning("Mainload.MenKe_Now 为空，无法实时添加门客");
                }
            }
            catch (Exception e)
            {
                statusMessage = LanguageManager.GetText("ErrorAddFailed") + e.Message;
                Logger.LogError("实时添加门客失败: " + e.Message);
                Logger.LogError("异常堆栈: " + e.StackTrace);
            }
        }
    }
}
