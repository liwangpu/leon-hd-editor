using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using BambooCook.Logic;
using BambooUploader.Logic;

namespace BambooEditor.Logic
{
    public class BambooEditor
    {
        public static BambooEditor Instance { get; } = new BambooEditor();
        public bool Logined { get; set; }
        public ApiMan api = null;

        public List<CategoryModel> rootCategories = new List<CategoryModel>();
        Dictionary<string, CategoryModel> categoryMap = new Dictionary<string, CategoryModel>();
        Dictionary<string, string> categoryNameToIdMap = new Dictionary<string, string>();
        public Uri serverBase;

        public SlotFaceDefineSet faceDefines;
        public SlotPredefines SlotPredefines;
        public ColorSheetSet colorSheetSet;

        public event Action CategoryUpdated;

        BambooEditor()
        {
        }

        public async Task<string> Login(Uri serverBase, string acc, string pwd)
        {
            this.serverBase = serverBase;
            api = new ApiMan(serverBase);
            var md5 = System.Security.Cryptography.MD5.Create();
            var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(pwd));
            pwd = BitConverter.ToString(bytes).Replace("-", "").ToLower();
            var result = await api.PostAsync("token", new GetTokenModel { Account = acc, Password = pwd });
            if (result.IsSuccess && result.Content != null)
            {
                GetTokenResModel msg = Newtonsoft.Json.JsonConvert.DeserializeObject<GetTokenResModel>(result.Content);
                if (string.IsNullOrEmpty(msg.Token))
                    return msg.Message;
                api.SetToken(msg.Token);
                string roleid = await CheckAccountRole();
                if (roleid.ToLower().IndexOf("admin") < 0)
                    return "manager account required";
                await loadInitData();
                return "";
            }
            return result.StatusCode + " " + result.Content;
        }

        async Task<int> loadInitData()
        {
            LoadPredefineSlots();
            LoadFaceDefines();
            LoadColorSheetSet();
            await LoadAllCategories();
            return 0;
        }

        async Task<string> CheckAccountRole()
        {
            var profile = await api.GetAsync<AccountProfile>("/account/profile");
            if (profile == null || profile.Content == null)
                return "";
            return profile.Content.roleId;
        }

        void LoadPredefineSlots()
        {
            string fileName = "slotpredefines.txt";
            try
            {
                StreamReader sr = new StreamReader(fileName);
                string json = sr.ReadToEnd();
                sr.Close();
                SlotPredefines = Newtonsoft.Json.JsonConvert.DeserializeObject<SlotPredefines>(json);
                if(SlotPredefines == null || SlotPredefines.Slots == null || SlotPredefines.Slots.Count == 0)
                {
                    UseDefaultSlotDefines();
                }
            }catch
            {
                UseDefaultSlotDefines();
            }
        }

        void LoadColorSheetSet()
        {
            string fileName = "colorsheetset.txt";
            try
            {
                StreamReader sr = new StreamReader(fileName);
                string json = sr.ReadToEnd();
                sr.Close();
                colorSheetSet = Newtonsoft.Json.JsonConvert.DeserializeObject<ColorSheetSet>(json);
                if (colorSheetSet == null || colorSheetSet.Sheets == null)
                {
                    UseDefaultColorSheet();
                }
            }
            catch
            {
                UseDefaultColorSheet();
            }
        }

        void UseDefaultColorSheet()
        {
            colorSheetSet = new ColorSheetSet();
            colorSheetSet.Sheets = new List<ColorSheet>();

            ColorSheet defaultSheet = new ColorSheet();
            defaultSheet.Name = "system-colors";
            defaultSheet.Colors = new List<ColorModel>();
            Type t = typeof(Brushes);
            var ms = t.GetProperties();
            foreach (var item in ms)
            {
                ColorModel cm = new ColorModel();
                cm.Name = item.Name;
                cm.ColorBrush = item.GetValue(null, null) as Brush;
                cm.Color = ColorToString((cm.ColorBrush as SolidColorBrush).Color);
                defaultSheet.Colors.Add(cm);
            }
            colorSheetSet.Sheets.Add(defaultSheet);

            //string htmlcolors = "#F9EBEA,#F2D7D5,#E6B0AA,#D98880,#CD6155,#C0392B,#A93226,#922B21,#7B241C,#641E16,#FDEDEC,#FADBD8,#F5B7B1,#F1948A,#EC7063,#E74C3C,#CB4335,#B03A2E,#943126,#78281F,#F5EEF8,#EBDEF0,#D7BDE2,#C39BD3,#AF7AC5,#9B59B6,#884EA0,#76448A,#633974,#512E5F,#F4ECF7,#E8DAEF,#D2B4DE,#BB8FCE,#A569BD,#8E44AD,#7D3C98,#6C3483,#5B2C6F,#4A235A,#EAF2F8,#D4E6F1,#A9CCE3,#7FB3D5,#5499C7,#2980B9,#2471A3,#1F618D,#1A5276,#154360,#EBF5FB,#D6EAF8,#AED6F1,#85C1E9,#5DADE2,#3498DB,#2E86C1,#2874A6,#21618C,#1B4F72,#E8F8F5,#D1F2EB,#A3E4D7,#76D7C4,#48C9B0,#1ABC9C,#17A589,#148F77,#117864,#0E6251,#E8F6F3,#D0ECE7,#A2D9CE,#73C6B6,#45B39D,#16A085,#138D75,#117A65,#0E6655,#0B5345,#E9F7EF,#D4EFDF,#A9DFBF,#7DCEA0,#52BE80,#27AE60,#229954,#1E8449,#196F3D,#145A32,#EAFAF1,#D5F5E3,#ABEBC6,#82E0AA,#58D68D,#2ECC71,#28B463,#239B56,#1D8348,#186A3B,#FEF9E7,#FCF3CF,#F9E79F,#F7DC6F,#F4D03F,#F1C40F,#D4AC0D,#B7950B,#9A7D0A,#7D6608,#FEF5E7,#FDEBD0,#FAD7A0,#F8C471,#F5B041,#F39C12,#D68910,#B9770E,#9C640C,#7E5109,#FDF2E9,#FAE5D3,#F5CBA7,#F0B27A,#EB984E,#E67E22,#CA6F1E,#AF601A,#935116,#784212,#FBEEE6,#F6DDCC,#EDBB99,#E59866,#DC7633,#D35400,#BA4A00,#A04000,#873600,#6E2C00,#FDFEFE,#FBFCFC,#F7F9F9,#F4F6F7,#F0F3F4,#ECF0F1,#D0D3D4,#B3B6B7,#979A9A,#7B7D7D,#F8F9F9,#F2F3F4,#E5E7E9,#D7DBDD,#CACFD2,#BDC3C7,#A6ACAF,#909497,#797D7F,#626567,#F4F6F6,#EAEDED,#D5DBDB,#BFC9CA,#AAB7B8,#95A5A6,#839192,#717D7E,#5F6A6A,#4D5656,#F2F4F4,#E5E8E8,#CCD1D1,#B2BABB,#99A3A4,#7F8C8D,#707B7C,#616A6B,#515A5A,#424949,#EBEDEF,#D6DBDF,#AEB6BF,#85929E,#5D6D7E,#34495E,#2E4053,#283747,#212F3C,#1B2631,#EAECEE,#D5D8DC,#ABB2B9,#808B96,#566573,#2C3E50,#273746,#212F3D,#1C2833,#17202A";
            //colorSheetSet.Sheets.Add(createColorSheetWithSimpleColorList("html-colors", htmlcolors));
            string htmlcolornames = "#000000,Black;#0C090A,Night;#2C3539,Gunmetal;#2B1B17,Midnight;#34282C,Charcoal;#25383C,Dark_Slate_Grey;#3B3131,Oil;#413839,Black_Cat;#3D3C3A,Iridium;#463E3F,Black_Eel;#4C4646,Black_Cow;#504A4B,Gray_Wolf;#565051,Vampire_Gray;#5C5858,Gray_Dolphin;#625D5D,Carbon_Gray;#666362,Ash_Gray;#6D6968,Cloudy_Gray;#726E6D,Smokey_Gray;#736F6E,Gray;#837E7C,Granite;#848482,Battleship_Gray;#B6B6B4,Gray_Cloud;#D1D0CE,Gray_Goose;#E5E4E2,Platinum;#BCC6CC,Metallic_Silver;#98AFC7,Blue_Gray;#6D7B8D,Light_Slate_Gray;#657383,Slate_Gray;#616D7E,Jet_Gray;#646D7E,Mist_Blue;#566D7E,Marble_Blue;#737CA1,Slate_Blue;#4863A0,Steel_Blue;#2B547E,Blue_Jay;#2B3856,Dark_Slate_Blue;#151B54,Midnight_Blue;#000080,Navy_Blue;#342D7E,Blue_Whale;#15317E,Lapis_Blue;#151B8D,Denim_Dark_Blue;#0000A0,Earth_Blue;#0020C2,Cobalt_Blue;#0041C2,Blueberry_Blue;#2554C7,Sapphire_Blue;#1569C7,Blue_Eyes;#2B60DE,Royal_Blue;#1F45FC,Blue_Orchid;#6960EC,Blue_Lotus;#736AFF,Light_Slate_Blue;#357EC7,Windows_Blue;#368BC1,Glacial_Blue_Ice;#488AC7,Silk_Blue;#3090C7,Blue_Ivy;#659EC7,Blue_Koi;#87AFC7,Columbia_Blue;#95B9C7,Baby_Blue;#728FCE,Light_Steel_Blue;#2B65EC,Ocean_Blue;#306EFF,Blue_Ribbon;#157DEC,Blue_Dress;#1589FF,Dodger_Blue;#6495ED,Cornflower_Blue;#6698FF,Sky_Blue;#38ACEC,Butterfly_Blue;#56A5EC,Iceberg;#5CB3FF,Crystal_Blue;#3BB9FF,Deep_Sky_Blue;#79BAEC,Denim_Blue;#82CAFA,Light_Sky_Blue;#82CAFF,Day_Sky_Blue;#A0CFEC,Jeans_Blue;#B7CEEC,Blue_Angel;#B4CFEC,Pastel_Blue;#C2DFFF,Sea_Blue;#C6DEFF,Powder_Blue;#AFDCEC,Coral_Blue;#ADDFFF,Light_Blue;#BDEDFF,Robin_Egg_Blue;#CFECEC,Pale_Blue_Lily;#E0FFFF,Light_Cyan;#EBF4FA,Water;#F0F8FF,AliceBlue;#F0FFFF,Azure;#CCFFFF,Light_Slate;#93FFE8,Light_Aquamarine;#9AFEFF,Electric_Blue;#7FFFD4,Aquamarine;#00FFFF,Cyan_or_Aqua;#7DFDFE,Tron_Blue;#57FEFF,Blue_Zircon;#8EEBEC,Blue_Lagoon;#50EBEC,Celeste;#4EE2EC,Blue_Diamond;#81D8D0,Tiffany_Blue;#92C7C7,Cyan_Opaque;#77BFC7,Blue_Hosta;#78C7C7,Northern_Lights_Blue;#48CCCD,Medium_Turquoise;#43C6DB,Turquoise;#46C7C7,Jellyfish;#7BCCB5,Blue_green;#43BFC7,Macaw_Blue_Green;#3EA99F,Light_Sea_Green;#3B9C9C,Dark_Turquoise;#438D80,Sea_Turtle_Green;#348781,Medium_Aquamarine;#307D7E,Greenish_Blue;#5E7D7E,Grayish_Turquoise;#4C787E,Beetle_Green;#008080,Teal;#4E8975,Sea_Green;#78866B,Camouflage_Green;#848b79,Sage_Green;#617C58,Hazel_Green;#728C00,Venom_Green;#667C26,Fern_Green;#254117,Dark_Forest_Green;#306754,Medium_Sea_Green;#347235,Medium_Forest_Green;#437C17,Seaweed_Green;#387C44,Pine_Green;#347C2C,Jungle_Green;#347C17,Shamrock_Green;#348017,Medium_Spring_Green;#4E9258,Forest_Green;#6AA121,Green_Onion;#4AA02C,Spring_Green;#41A317,Lime_Green;#3EA055,Clover_Green;#6CBB3C,Green_Snake;#6CC417,Alien_Green;#4CC417,Green_Apple;#52D017,Yellow_Green;#4CC552,Kelly_Green;#54C571,Zombie_Green;#99C68E,Frog_Green;#89C35C,Green_Peas;#85BB65,Dollar_Bill_Green;#8BB381,Dark_Sea_Green;#9CB071,Iguana_Green;#B2C248,Avocado_Green;#9DC209,Pistachio_Green;#A1C935,Salad_Green;#7FE817,Hummingbird_Green;#59E817,Nebula_Green;#57E964,Stoplight_Go_Green;#64E986,Algae_Green;#5EFB6E,Jade_Green;#00FF00,Green;#5FFB17,Emerald_Green;#87F717,Lawn_Green;#8AFB17,Chartreuse;#6AFB92,Dragon_Green;#98FF98,Mint_green;#B5EAAA,Green_Thumb;#C3FDB8,Light_Jade;#CCFB5D,Tea_Green;#B1FB17,Green_Yellow;#BCE954,Slime_Green;#EDDA74,Goldenrod;#EDE275,Harvest_Gold;#FFE87C,Sun_Yellow;#FFFF00,Yellow;#FFF380,Corn_Yellow;#FFFFC2,Parchment;#FFFFCC,Cream;#FFF8C6,Lemon_Chiffon;#FFF8DC,Cornsilk;#F5F5DC,Beige;#FBF6D9,Blonde;#FAEBD7,AntiqueWhite;#F7E7CE,Champagne;#FFEBCD,BlanchedAlmond;#F3E5AB,Vanilla;#ECE5B6,Tan_Brown;#FFE5B4,Peach;#FFDB58,Mustard;#FFD801,Rubber_Ducky_Yellow;#FDD017,Bright_Gold;#EAC117,Golden_brown;#F2BB66,Macaroni_and_Cheese;#FBB917,Saffron;#FBB117,Beer;#FFA62F,Cantaloupe;#E9AB17,Bee_Yellow;#E2A76F,Brown_Sugar;#DEB887,BurlyWood;#FFCBA4,Deep_Peach;#C9BE62,Ginger_Brown;#E8A317,School_Bus_Yellow;#EE9A4D,Sandy_Brown;#C8B560,Fall_Leaf_Brown;#D4A017,Orange_Gold;#C2B280,Sand;#C7A317,Cookie_Brown;#C68E17,Caramel;#B5A642,Brass;#ADA96E,Khaki;#C19A6B,Camel_brown;#CD7F32,Bronze;#C88141,Tiger_Orange;#C58917,Cinnamon;#AF9B60,Bullet_Shell;#AF7817,Dark_Goldenrod;#B87333,Copper;#966F33,Wood;#806517,Oak_Brown;#827839,Moccasin;#827B60,Army_Brown;#786D5F,Sandstone;#493D26,Mocha;#483C32,Taupe;#6F4E37,Coffee;#835C3B,Brown_Bear;#7F5217,Red_Dirt;#7F462C,Sepia;#C47451,Orange_Salmon;#C36241,Rust;#C35817,Red_Fox;#C85A17,Chocolate;#CC6600,Sedona;#E56717,Papaya_Orange;#E66C2C,Halloween_Orange;#F87217,Pumpkin_Orange;#F87431,Construction_Cone_Orange;#E67451,Sunrise_Orange;#FF8040,Mango_Orange;#F88017,Dark_Orange;#FF7F50,Coral;#F88158,Basket_Ball_Orange;#F9966B,Light_Salmon;#E78A61,Tangerine;#E18B6B,Dark_Salmon;#E77471,Light_Coral;#F75D59,Bean_Red;#E55451,Valentine_Red;#E55B3C,Shocking_Orange;#FF0000,Red;#FF2400,Scarlet;#F62217,Ruby_Red;#F70D1A,Ferrari_Red;#F62817,Fire_Engine_Red;#E42217,Lava_Red;#E41B17,Love_Red;#DC381F,Grapefruit;#C34A2C,Chestnut_Red;#C24641,Cherry_Red;#C04000,Mahogany;#C11B17,Chilli_Pepper;#9F000F,Cranberry;#990012,Red_Wine;#8C001A,Burgundy;#954535,Chestnut;#7E3517,Blood_Red;#8A4117,Sienna;#7E3817,Sangria;#800517,Firebrick;#810541,Maroon;#7D0541,Plum_Pie;#7E354D,Velvet_Maroon;#7D0552,Plum_Velvet;#7F4E52,Rosy_Finch;#7F5A58,Puce;#7F525D,Dull_Purple;#B38481,Rosy_Brown;#C5908E,Khaki_Rose;#C48189,Pink_Bow;#C48793,Lipstick_Pink;#E8ADAA,Rose;#ECC5C0,Rose_Gold;#EDC9AF,Desert_Sand;#FDD7E4,Pig_Pink;#FCDFFF,Cotton_Candy;#FFDFDD,Pink_Bubble_Gum;#FBBBB9,Misty_Rose;#FAAFBE,Pink;#FAAFBA,Light_Pink;#F9A7B0,Flamingo_Pink;#E7A1B0,Pink_Rose;#E799A3,Pink_Daisy;#E38AAE,Cadillac_Pink;#F778A1,Carnation_Pink;#E56E94,Blush_Red;#F660AB,Hot_Pink;#FC6C85,Watermelon_Pink;#F6358A,Violet_Red;#F52887,Deep_Pink;#E45E9D,Pink_Cupcake;#E4287C,Pink_Lemonade;#F535AA,Neon_Pink;#FF00FF,Magenta;#E3319D,Dimorphotheca_Magenta;#F433FF,Bright_Neon_Pink;#D16587,Pale_Violet_Red;#C25A7C,Tulip_Pink;#CA226B,Medium_Violet_Red;#C12869,Rogue_Pink;#C12267,Burnt_Pink;#C25283,Bashful_Pink;#C12283,Dark_Carnation_Pink;#B93B8F,Plum;#7E587E,Viola_Purple;#571B7E,Purple_Iris;#583759,Plum_Purple;#4B0082,Indigo;#461B7E,Purple_Monster;#4E387E,Purple_Haze;#614051,Eggplant;#5E5A80,Grape;#6A287E,Purple_Jam;#7D1B7E,Dark_Orchid;#A74AC7,Purple_Flower;#B048B5,Medium_Orchid;#6C2DC7,Purple_Amethyst;#842DCE,Dark_Violet;#8D38C9,Violet;#7A5DC7,Purple_Sage_Bush;#7F38EC,Lovely_Purple;#8E35EF,Purple;#893BFF,Aztech_Purple;#8467D7,Medium_Purple;#A23BEC,Jasmine_Purple;#B041FF,Purple_Daffodil;#C45AEC,Tyrian_Purple;#9172EC,Crocus_Purple;#9E7BFF,Purple_Mimosa;#D462FF,Heliotrope_Purple;#E238EC,Crimson;#C38EC7,Purple_Dragon;#C8A2C8,Lilac;#E6A9EC,Blush_Pink;#E0B0FF,Mauve;#C6AEC7,Wisteria_Purple;#F9B7FF,Blossom_Pink;#D2B9D3,Thistle;#E9CFEC,Periwinkle;#EBDDE2,Lavender_Pinocchio;#E3E4FA,Lavender_blue;#FDEEF4,Pearl;#FFF5EE,SeaShell;#FEFCFF,Milk_White;#FFFFFF,White;";
            colorSheetSet.Sheets.Add(createColorSheetWithColorNameList("html-colors", htmlcolornames));

            // http://www.pantoneguides.cn/sehao/uncoated.html
            // http://www.pantoneguides.cn/sehao/coated.html
            string PANTONE_U = "#fce528,yellow_U;#fdd920,yellow_012_U;#e8723b,orange_021U;#df615c,warm_red_U;#d45661,red_032U;#b94a7b,Rubine_Red_U;#c6529e,Rhodamine_Red_U;#a656b7,Purple_U;#6d58ad,Violet_U;#3f46a2,Blue_072U;#3d498f,Reflex_Blue_U;#2982c1,Process_Blue;#40a887,Green_U;#5f5b56,Black_U;#fbe541,Process_Yellow_U;#bc4e82,Process_Magenta_U;#4c9dd3,Process_Cyan_U;#555251,Process_Black;#ffe139,Hexa;#ef7d3f,Hexa;#c1408e,Hexa;#4095ce,Hexa;#5eaf6a,Hexa;#52504f,Hexa;#f7ee7e,100U;#f9ef71,101U;#fbeb4a,102U;#fce528,Yellow_U;#b1a139,103U;#948a41,104U;#7e794d,105U;#f9e96e,106U;#fae45f,107U;#fadb4e,108U;#f9c62f,109U;#bf9f37,110U;#958540,111U;#847a44,112U;#f9e169,113U;#f9db5d,114U;#f9d050,115U;#f6bb34,116U;#ac8f3c,117U;#988441,118U;#80784d,119U;#f8da74,120U;#f8d26a,121U;#f8c458,122U;#f5af3d,123U;#cb943c,124U;#9c8144,125U;#8a7847,126U;#f7e6a0,1205U;#f8df8c,1215U;#f6bd5e,1225U;#f4ac49,1235U;#b08e4c,1245U;#98824d,1255U;#85774f,1265U;#f2e083,127U;#efd36c,128U;#e5b350,129U;#de993d,130U;#b4873d,131U;#8c7740,132U;#736a47,133U;#f7d480,134U;#f7c571,135U;#f4ad58,136U;#f19e45,137U;#c4803d,138U;#997643,139U;#7d6e4b,140U;#f7d292,1345U;#f7c884,1355U;#f4ae65,1365U;#f09a4e,1375U;#bb7f48,1385U;#8f7149,1395U;#78684c,1405U;#e9c070,141U;#e4b063,142U;#e0a157,143U;#d88a47,144U;#b47d44,145U;#8f7245,146U;#756c50,147U;#f6c68a,148U;#f5b778,149U;#ef9456,150U;#eb8649,151U;#c67743,152U;#9b6c46,153U;#876748,154U;#f7a871,1485U;#f4945b,1495U;#ef8348,1505U;#e8723b,Orange_021U;#af6840,1525U;#8d6548,1535U;#725d48,1545U;#eace9b,155U;#e4ba86,156U;#d89364,157U;#d17f53,158U;#b5744e,159U;#916b4b,160U;#75654e,161U;#f5c4a5,1555U;#f2ad89,1565U;#ee956f,1575U;#e98258,1585U;#ba6f4e,1595U;#956a51,1605U;#866450,1615U;#f5bea1,162U;#ef9a7a,163U;#ea8464,164U;#e67552,165U;#c66b4e,166U;#a2664e,167U;#7d6050,168U;#f1a38e,1625U;#ee937d,1635U;#ea816a,1645U;#e47155,1655U;#c5664f,1665U;#986351,1675U;#855f50,1685U;#f3b7ae,169U;#ed8d80,170U;#e77769,171U;#e16854,172U;#b56252,173U;#8c5b4f,174U;#785c53,175U;#f4b0b7,176U;#ee8b91,177U;#e77476,178U;#df615c,Warm_Red_U;#c35d5a,179U;#a55c5b,180U;#7d5755,181U;#efa3b0,1765U;#e88997,1775U;#dc6571,1785U;#d75d64,1788U;#bd5a5e,1795U;#9a5657,1805U;#815655,1815U;#eeb7c3,1767U;#e38091,1777U;#db6777,1787U;#d45661,Red_032U;#b1545c,1797U;#95585f,1807U;#6d5555,1817U;#efb5c4,182U;#e58ca1,183U;#dc6f83,184U;#cd5061,185U;#b3515e,186U;#9a535c,187U;#7a5459,188U;#e9a5bb,189U;#e0829c,190U;#d5657e,191U;#ca5067,192U;#a54f5f,193U;#89525e,194U;#75545b,195U;#e8aec4,1895U;#e294b0,1905U;#d26685,1915U;#c64e6c,1925U;#ac4b63,1935U;#924e61,1945U;#865160,1955U;#e5c2c8,196U;#da9caa,197U;#c96b7d,198U;#bd5265,199U;#a24f5e,200U;#8f5661,201U;#7d545d,202U;#dea9c1,203U;#d07f9f,204U;#c56182,205U;#b84969,206U;#974d65,207U;#805465,208U;#755561,209U;#e89bc4,210U;#df80af,211U;#d86d9e,212U;#c95482,213U;#b14f76,214U;#95506d,215U;#7b5464,216U;#e4bcd8,217U;#d387b7,218U;#c35d91,219U;#b94a7b,Rubine_Red_U;#984c72,220U;#8a4c6c,221U;#79576b,222U;#e3a2d3,223U;#d984c0,224U;#cb60a1,225U;#ba4584,226U;#994777,227U;#814f6f,228U;#735566,229U;#eaabd9,230U;#dd7fc4,231U;#d164af,232U;#c6529e,Rhodamine_Red_U;#b04f8f,233U;#96517f,234U;#834f72,235U;#e2a3da,236U;#d784cb,237U;#cb69ba,238U;#bd50a6,239U;#a55093,240U;#945086,241U;#7e5573,242U;#eac4e4,2365U;#d083cd,2375U;#c368bb,2385U;#b756ae,2395U;#a2529a,2405U;#94518d,2415U;#86527f,2425U;#dcaade,243U;#d394d6,244U;#c87ecb,245U;#b257b2,246U;#a04e9e,247U;#8c4f8b,248U;#795576,249U;#debbe2,250U;#ca92d8,251U;#b46cc5,252U;#a656b7,Purple_U;#9e56af,253U;#8b5499,254U;#77577d,255U;#d0b9db,256U;#b89cc9,257U;#9677aa,258U;#866899,259U;#7e658a,260U;#745e7c,261U;#705e73,262U;#cbabe2,2562U;#ba92da,2572U;#a476ca,2582U;#9360bb,2592U;#8559a5,2602U;#78588b,2612U;#735d7a,2622U;#ba9cd0,2563U;#a787c3,2573U;#9775b4,2583U;#7e5d9a,2593U;#75588b,2603U;#715684,2613U;#6b5579,2623U;#b099d2,2567U;#967cbf,2577U;#856bb0,2587U;#745a9f,2597U;#705892,2607U;#6d578a,2617U;#6a5780,2627U;#d5c8e8,263U;#b1a0df,264U;#8d79cc,265U;#7b63bc,266U;#6f5ba4,267U;#69598a,268U;#67597d,269U;#bfb4e3,2635U;#aea0dd,2645U;#9484cf,2655U;#7f6cbf,2665U;#6d58ad,Violet_U;#655398,2685U;#665a7d,2695U;#acadd9,270U;#9c9dd0,271U;#8585be,272U;#5e5890,273U;#585381,274U;#554f76,275U;#59546b,276U;#b5b3e4,2705U;#9794d8,2715U;#827cca,2725U;#5b51a3,2735U;#56508e,2745U;#544f82,2755U;#524e75,2765U;#cdd2ea,2706U;#a4ace2,2716U;#7276c6,2726U;#5b5aad,2736U;#56589a,2746U;#555784,2756U;#545675,2766U;#c4d4ee,2707U;#afc5ec,2717U;#6a86d8,2727U;#3f46a2,Blue_072U;#424b81,2747U;#434b74,2757U;#4c5067,2767U;#b1c1e6,2708U;#7183ce,2718U;#5663b4,2728U;#454995,2738U;#484d80,2748U;#484d76,2758U;#484b67,2768U;#b3cbe9,277U;#99b6e5,278U;#7191d6,279U;#3d498f,Reflex_Blue_U;#445082,280U;#444e73,281U;#444a64,282U;#adc9ea,283U;#8aaee3,284U;#5e81cc,285U;#4059a4,286U;#40538d,287U;#41507e,288U;#454d64,289U;#b9d6ea,290U;#99bfe8,291U;#7ba7e0,292U;#3d5ea8,293U;#435a87,294U;#435474,295U;#475161,296U;#9cc7e8,2905U;#7eafe0,2915U;#6094d4,2925U;#3b61a9,2935U;#3d5b8e,2945U;#435875,2955U;#445164,2965U;#95c8e9,297U;#78b4e3,298U;#5392d2,299U;#346db3,300U;#3a6290,301U;#425c74,302U;#455664,303U;#a5d6e8,2975U;#72b7e3,2985U;#529cd7,2995U;#3076ba,3005U;#376792,3015U;#405d72,3025U;#435562,3035U;#a8dce9,304U;#88cce9,305U;#61b4e1,306U;#2982c1,Process_Blue_U;#2e73a1,307U;#37617c,308U;#415560,309U;#90d1e4,310U;#71c0db,311U;#55aecf,312U;#2b8eb3,313U;#2c7a98,314U;#386678,315U;#445860,316U;#88cfdc,3105U;#6cbfd2,3115U;#4fabc1,3125U;#2b91aa,3135U;#357688,3145U;#3a6875,3155U;#3d5d67,3165U;#c1e8e4,317U;#95d8da,318U;#79cacf,319U;#389da6,320U;#368188,321U;#3e6d73,322U;#426165,323U;#a5dad4,324U;#7bc3bb,325U;#5caa9f,326U;#3f8c7d,327U;#427972,328U;#446f6a,329U;#4c6060,330U;#99ddd3,3242U;#7dd0c5,3252U;#5bbcaf,3262U;#3da498,3272U;#478a83,3282U;#4b6c69,3292U;#4f6461,3302U;#96decd,3245U;#79d1bd,3255U;#5fc1ab,3265U;#43aa93,3275U;#4d9182,3285U;#518479,3295U;#526862,3305U;#97d2c0,3248U;#7ec3ae,3258U;#64af97,3268U;#4f9c80,3278U;#4d8874,3288U;#4a6f65,3298U;#4e605c,3308U;#b1e8d2,331U;#9ee1c8,332U;#74cfb1,333U;#40a887,Green_U;#449179,334U;#4c7c6d,335U;#4f7066,336U;#9ad3b7,337U;#7bbf9e,338U;#61a986,339U;#559c78,340U;#4d7765,341U;#4f6e61,342U;#50635d,343U;#9ae0b9,3375U;#81d4a6,3385U;#70c893,3395U;#56b37e,3405U;#548169,3415U;#51715f,3425U;#4f6258,3435U;#a8daaf,344U;#96d1a1,345U;#7bbe89,346U;#599e69,347U;#51845b,348U;#527058,349U;#526053,350U;#a8e3aa,351U;#98dd9d,352U;#88d58e,353U;#54a75d,354U;#539358,355U;#527a55,356U;#586b57,357U;#a7d688,358U;#9ccf7e,359U;#81bc69,360U;#71ac59,361U;#658e51,362U;#648451,363U;#5e754d,364U;#c8e490,365U;#b9dd81,366U;#a4d16f,367U;#7fb34e,368U;#769f4c,369U;#6d864b,370U;#67714e,371U;#dbee91,372U;#c7e772,373U;#b8e164,374U;#91c743,375U;#80a541,376U;#798b46,377U;#6d734d,378U;#e3eb78,379U;#d5e364,380U;#bfd64f,381U;#a6c43c,382U;#8e9941,383U;#828743,384U;#7a7a4f,385U;#edef7a,386U;#e4eb63,387U;#d9e753,388U;#c3da36,389U;#9ba737,390U;#868842,391U;#7b7a47,392U;#f4f289,393U;#eceb5a,394U;#e9ea51,395U;#d9e031,396U;#a8a836,397U;#95933a,398U;#888541,399U;#f6f183,3935U;#f5ec5d,3945U;#f0e743,3955U;#e9e12d,3965U;#aca23b,3975U;#928b44,3985U;#787347,3995U;#c2beb5,400U;#b2aca5,401U;#9d9892,402U;#918c87,403U;#827e79,404U;#706c67,405U;#5f5b56,Black_U;#beb7b0,406U;#a79f9a,407U;#958e89,408U;#8a8380,409U;#827b79,410U;#6e6967,411U;#605a57,412U;#bcbbb0,413U;#a7a79d,414U;#97968e,415U;#8a8a83,416U;#82827c,417U;#74746f,418U;#575753,419U;#c7c7c3,420U;#aeafad,421U;#a1a2a1,422U;#919293,423U;#7e8081,424U;#707173,425U;#59595a,426U;#c8cac9,427U;#afb3b4,428U;#94999d,429U;#7e8389,430U;#6c7177,431U;#62666b,432U;#56575c,433U;#d0c8c8,434U;#b5aaac,435U;#a19599,436U;#83787c,437U;#706669,438U;#685f61,439U;#635d5c,440U;#bfc5c1,441U;#aaafae,442U;#979c9d,443U;#7f8486,444U;#6c6f72,445U;#66696a,446U;#5f5f5e,447U;#e3dfd8,Warm_Gray_1U;#d4d0c8,Warm_Gray_2U;#c0bbb3,Warm_Gray_3U;#b1aca5,Warm_Gray_4U;#a59f99,Warm_Gray_5U;#9a9590,Warm_Gray_6U;#928c88,Warm_Gray_7U;#8a8581,Warm_Gray_8U;#847f7b,Warm_Gray_9U;#7c7773,Warm_Gray_10U;#75716e,Warm_Gray_11U;#e1e0db,Cool_Gray_1U;#d3d3cf,Cool_Gray_2U;#c4c5c3,Cool_Gray_3U;#b4b5b4,Cool_Gray_4U;#abacac,Cool_Gray_5U;#a2a3a4,Cool_Gray_6U;#97989a,Cool_Gray_7U;#8e8f92,Cool_Gray_8U;#858689,Cool_Gray_9U;#7e8083,Cool_Gray_10U;#75767a,Cool_Gray_11U;#615e53,Black_2U;#5a5d59,Black_3U;#615a53,Black_4U;#62595b,Black_5U;#53565f,Black_6U;#6b6968,Black_7U;#6a6758,448U;#6e6c58,449U;#737358,450U;#919176,451U;#aaaa8c,452U;#bbba9c,453U;#cac9ab,454U;#726b4f,4485U;#857d5d,4495U;#978e6c,4505U;#aca37c,4515U;#bdb48b,4525U;#ccc49c,4535U;#d7d0a9,4545U;#736d4c,455U;#8f834c,456U;#a19049,457U;#c9ba6a,458U;#d9cc7b,459U;#e3d98b,460U;#eae39e,461U;#716956,462U;#7d7055,463U;#917f5b,464U;#a79572,465U;#b6a57e,466U;#c7b991,467U;#d6cba5,468U;#6e5a4a,4625U;#8c7261,4635U;#947a69,4645U;#a78a78,4655U;#bfa48f,4665U;#d2b9a3,4675U;#dfcdb8,4685U;#735e4d,469U;#906a50,470U;#ac7756,471U;#c89071,472U;#dba987,473U;#e6bd9d,474U;#eccaaa,475U;#6c5a51,4695U;#7d6962,4705U;#907b73,4715U;#a18c82,4725U;#b49f94,4735U;#c6b3a6,4745U;#d5c6b8,4755U;#6a5f56,476U;#705f55,477U;#836b5e,478U;#9e8678,479U;#baa392,480U;#c8b4a1,481U;#d6c6b3,482U;#755d53,483U;#8f5d52,484U;#c35c52,485U;#d78d80,486U;#dba08f,487U;#e3b7a5,488U;#e8c8b7,489U;#765a5c,490U;#815b5f,491U;#8e5f64,492U;#b57d89,493U;#d19aa7,494U;#deadb9,495U;#e8c1c9,496U;#6c5958,497U;#785c5c,498U;#826060,499U;#af878d,500U;#c8a0a6,501U;#dab8bd,502U;#e6cbce,503U;#6c5757,4975U;#886d72,4985U;#92767c,4995U;#a7888f,5005U;#bda0a5,5015U;#d1b7ba,5025U;#e0cccc,5035U;#765c61,504U;#7c5a62,505U;#895f6b,506U;#b38295,507U;#ca99ac,508U;#d7aabb,509U;#e3bdc9,510U;#6a5266,511U;#7a557c,512U;#8b5a94,513U;#b680bb,514U;#cd9dce,515U;#dbb2d8,516U;#e6c6df,517U;#695766,5115U;#776577,5125U;#847286,5135U;#9c899d,5145U;#b6a4b5,5155U;#c8b8c6,5165U;#d7cbd3,5175U;#675b69,518U;#6a5973,519U;#745f83,520U;#9e89af,521U;#af9cc0,522U;#c1b0ce,523U;#d3c5da,524U;#695b63,5185U;#736571,5195U;#897a87,5205U;#9c8c9a,5215U;#b5a5b1,5225U;#d1c4cc,5235U;#d9ced4,5245U;#685674,525U;#70588c,526U;#7e5eab,527U;#a184cb,528U;#bca2db,529U;#cbb3e2,530U;#d8c3e5,531U;#595469,5255U;#68657e,5265U;#75738d,5275U;#918fa9,5285U;#aba9bf,5295U;#c2c0d1,5305U;#d2d1dc,5315U;#5b5c65,532U;#5b6172,533U;#606882,534U;#919bb4,535U;#a8b0c7,536U;#bfc6d5,537U;#d2d6df,538U;#464e5e,539U;#45536e,540U;#44597a,541U;#7292b9,542U;#8eadce,543U;#a3bed9,544U;#c2d4e5,545U;#494e5a,5395U;#5d6a78,5405U;#6d7c8a,5415U;#8494a2,5425U;#a1b0bc,5435U;#bac6ce,5445U;#ced6db,5455U;#4a535b,546U;#4b5c68,547U;#49606d,548U;#7e9dad,549U;#8dacba,550U;#a4bfcb,551U;#bed2d9,552U;#3f4b53,5463U;#586e75,5473U;#6c858c,5483U;#86a0a6,5493U;#a6bec2,5503U;#bdd1d2,5513U;#ccdbda,5523U;#565d5d,5467U;#626c6d,5477U;#717d7d,5487U;#899594,5497U;#a4b0ad,5507U;#b0bbb7,5517U;#c3ccc7,5527U;#576259,553U;#586b5d,554U;#607a67,555U;#819e8d,556U;#97b3a0,557U;#a5beab,558U;#bed1c0,559U;#4a5350,5535U;#697671,5545U;#7d8b85,5555U;#92a19a,5565U;#a5b5ac,5575U;#bac8bf,5585U;#ced8cf,5595U;#55605d,560U;#586f6b,561U;#60827d,562U;#90b5ad,563U;#a1c5bc,564U;#b6d5cb,565U;#c4dfd4,566U;#5b635b,5605U;#656e67,5615U;#768078,5625U;#8e998f,5635U;#a1aba0,5645U;#b6bfb3,5655U;#c5ccbf,5665U;#586762,567U;#58766f,568U;#5b897f,569U;#84b8ab,570U;#9ecdc1,571U;#b4dcd0,572U;#c4e3d7,573U;#686e55,574U;#6e7c54,575U;#788e5a,576U;#97ae79,577U;#b0c48e,578U;#c0d19c,579U;#d2ddb0,580U;#646853,5743U;#727662,5753U;#7e836c,5763U;#979c82,5773U;#acb094,5783U;#c1c4a8,5793U;#cdd0b6,5803U;#686b4d,5747U;#7a7d5c,5757U;#8f936e,5767U;#a3a67d,5777U;#bcbf95,5787U;#c7caa1,5797U;#d7d9b6,5807U;#706f4b,581U;#87894a,582U;#a2ac49,583U;#cdd36c,584U;#dde183,585U;#e4e794,586U;#e8ea9f,587U;#68674a,5815U;#827f5d,5825U;#8f8d68,5835U;#a4a178,5845U;#b9b68b,5855U;#cbc99f,5865U;#d7d5af,5875U;#f1eea9,600U;#f1eda0,601U;#f1ed96,602U;#eee677,603U;#e8dc5c,604U;#d9c941,605U;#c8b736,606U;#f0edb2,607U;#ede9a4,608U;#e7e290,609U;#dcd578,610U;#c5bb5c,611U;#b5ab51,612U;#9d9344,613U;#e0dcab,614U;#dad6a3,615U;#cac48c,616U;#b9b37b,617U;#a49e6a,618U;#8e885a,619U;#817c50,620U;#cddad0,621U;#bacbc1,622U;#a0b5ab,623U;#8a9e95,624U;#72857d,625U;#5d6e68,626U;#4c5955,627U;#c8e2e7,628U;#abd4df,629U;#8cc2d5,630U;#79b1c9,631U;#5f98b4,632U;#4d809c,633U;#3f6e8a,634U;#bce2ed,635U;#a2d6ea,636U;#82c4e3,637U;#64afd8,638U;#4e9ac7,639U;#3983b2,640U;#3278a7,641U;#cdd8e4,642U;#b6c5d9,643U;#94a8c5,644U;#7f94b4,645U;#6c7fa0,646U;#5a6989,647U;#4d5874,648U;#d5dce7,649U;#c4cedf,650U;#a4b2cd,651U;#8394b4,652U;#657596,653U;#515b7a,654U;#4d5573,655U;#d7dfec,656U;#c7d3ea,657U;#aebfe3,658U;#8ea4d6,659U;#677db8,660U;#4e5e99,661U;#434d83,662U;#ddd9e3,663U;#d2ccdc,664U;#bfb8ce,665U;#aba3be,666U;#877f9d,667U;#7c7391,668U;#6e6481,669U;#e6d2e3,670U;#e0c4dc,671U;#d1a2c6,672U;#c48ab4,673U;#b4759f,674U;#a4638c,675U;#905175,676U;#dfcddb,677U;#d7bfd2,678U;#ccaec5,679U;#b08aa6,680U;#956f8a,681U;#846079,682U;#735166,683U;#e1cad8,684U;#d8bbce,685U;#c39eb8,686U;#af88a3,687U;#97718c,688U;#856179,689U;#79576a,690U;#e5d2d6,691U;#d9bcc2,692U;#c5a1aa,693U;#b38d96,694U;#96737c,695U;#85646c,696U;#7a5a60,697U;#ebd0d7,698U;#e5bec8,699U;#daa2b0,700U;#ca8998,701U;#b67482,702U;#9c5d69,703U;#90565f,704U;#eed4dc,705U;#ecc7d2,706U;#e5aab9,707U;#dd92a3,708U;#cf7686,709U;#bf6170,710U;#b35763,711U;#f1caa4,712U;#efbc90,713U;#ebae7f,714U;#de9464,715U;#d18457,716U;#bf7349,717U;#b36a41,718U;#e7cbad,719U;#e1c09f,720U;#d4ab88,721U;#bc906e,722U;#ad8262,723U;#936c50,724U;#856246,725U;#e3ceb6,726U;#d5ba9f,727U;#be9f84,728U;#b09178,729U;#9a7d65,730U;#876c56,731U;#77604b,732U;#f2e2b0,7401U;#ebdd9d,7402U;#e3cb85,7403U;#edcd51,7404U;#d3ac2f,7405U;#deb63d,7406U;#c1a171,7407U;#d49435,7408U;#e0ac57,7409U;#e9a77f,7410U;#d4a273,7411U;#bd8a64,7412U;#c2855b,7413U;#b4825d,7414U;#d9b4a1,7415U;#cb756a,7416U;#c5685e,7417U;#a9686b,7418U;#90616a,7419U;#925969,7420U;#6e4c59,7421U;#edd1d8,7422U;#c7758d,7423U;#ba577b,7424U;#a55c73,7425U;#9c5569,7426U;#894e58,7427U;#785d68,7428U;#e0c9d5,7429U;#d3b1c3,7430U;#c79eb3,7431U;#ae7d94,7432U;#a16c82,7433U;#95657a,7434U;#7c5468,7435U;#e4d3e4,7436U;#cab6d4,7437U;#bf95cf,7438U;#b49dc2,7439U;#a38db0,7440U;#9c7fc8,7441U;#8860bb,7442U;#dddcea,7443U;#bbbdde,7444U;#a5a3c6,7445U;#9292c9,7446U;#716988,7447U;#665f6d,7448U;#574c56,7449U;#bdc2d9,7450U;#99ade2,7451U;#909add,7452U;#91a9dc,7453U;#7f96b6,7454U;#6972ac,7455U;#7e84b7,7456U;#cee4ed,7457U;#80a8c1,7458U;#6990ab,7459U;#308fc1,7460U;#6391c2,7461U;#5e7799,7462U;#58657c,7463U;#b7dad7,7464U;#7fc5b6,7465U;#53b0ba,7466U;#47a7b1,7467U;#51728d,7468U;#4b6a84,7469U;#55727f,7470U;#a2e0d7,7471U;#8bbdba,7472U;#78a599,7473U;#618c95,7474U;#798e91,7475U;#61757a,7476U;#64727b,7477U;#b7e8c3,7478U;#78c981,7479U;#6bc181,7480U;#68bb72,7481U;#66aa75,7482U;#5f735f,7483U;#536b5e,7484U;#e3ead4,7485U;#c5e39e,7486U;#a6dd7d,7487U;#8bcc61,7488U;#8eae75,7489U;#849c6d,7490U;#838c66,7491U;#c5ce94,7492U;#bfc79d,7493U;#a7b396,7494U;#818a5c,7495U;#76814b,7496U;#858274,7497U;#727561,7498U;#f1edca,7499U;#e3dbbb,7500U;#d9cdad,7501U;#bcaa86,7502U;#999377,7503U;#8b7e71,7504U;#827466,7505U;#eee3c4,7506U;#f4dbb3,7507U;#d3b288,7508U;#c7a37a,7509U;#ac8663,7510U;#937153,7511U;#866647,7512U;#dbb9ac,7513U;#caa796,7514U;#b18d7c,7515U;#906d5e,7516U;#7f6051,7517U;#776d6b,7518U;#6a635e,7519U;#e1c0b6,7520U;#b89f94,7521U;#9a736e,7522U;#997774,7523U;#94706a,7524U;#8e776d,7525U;#815b50,7526U;#dad7cb,7527U;#cfc8bf,7528U;#bbb2a9,7529U;#a79e94,7530U;#847c73,7531U;#6e665e,7532U;#645d55,7533U;#d3d0c3,7534U;#c0bcac,7535U;#ada99a,7536U;#b3b6b0,7537U;#a5a8a2,7538U;#9c9d9c,7539U;#6d7075,7540U;#e3e7e9,7541U;#b7c3ca,7542U;#b0b6bd,7543U;#9ea4ac,7544U;#7d838d,7545U;#696f78,7546U;#5c5f68,7547U;#439bc4,801U;#91d559,802U;#ffe64f,803U;#ff9f5b,804U;#f85b64,805U;#e02e98,806U;#b32fad,807U;#5bae91,808U;#e3e249,809U;#ffca4b,810U;#ff775e,811U;#e74174,812U;#c130a0,813U;#7d68c4,814U;#a4936c,871U;#a69270,872U;#a58f6f,873U;#a99277,874U;#aa8f77,875U;#a88872,876U;#b3b5b8,877U;#ada399,8003U;#aba39f,8021U;#a6969c,8062U;#aba4b0,8100U;#99a5b3,8201U;#a1aaaa,8281U;#a5aba2,8321U";
            colorSheetSet.Sheets.Add(createColorSheetWithColorNameList("潘通(PANTONE)-Uncoated", PANTONE_U));

            string PANTONE_C = "#f5de00,yellow_C_(黄色_C);#f6d400,yellow_012C_(黄色_012C);#e05800,orange_021C_(橙色_021C);#d6423d,warm_red_C_(暖红_C);#cc2d3b,red_032C_(红_032C);#ab005c,Rubine_Red_C_(宝石红_C);#c01899,Rhodamine_Red_C_(玫瑰红_C);#9d37b6,Purple_C_(红紫_C);#42119c,Violet_C_(蓝紫_C);#001ea3,Blue_072C_(蓝_072C);#002891,Reflex_Blue_C_(射光蓝_C);#0086ca,Process_Blue_(四色蓝_C);#00ac84,Green_C_(绿_C);#2d2b28,Black_C_(黑_C);#f3e200,Process_Yellow_C_(四色黄_C);#b20071,Process_Magenta_C_(四色品红_C);#0d9ed7,Process_Cyan_C_(四色青_C);#232323,Process_Black_(四色黑_C);#f0ec80,100C;#f2ec65,101C;#f5e600,102C;#f5de00,Yellow_C;#bdaa00,103C;#a79910,104C;#827930,105C;#f2e560,106C;#f2e03d,107C;#f3d800,108C;#f2d000,109C;#caa800,110C;#a28c00,111C;#8f7f1c,112C;#f1df5a,113C;#f1dd52,114C;#f2db51,115C;#f1c914,116C;#ba9800,117C;#a28600,118C;#80732b,119C;#f0dd75,120C;#f1d96b,121C;#f1d35b,122C;#efc742,123C;#d9aa19,124C;#ab8919,125C;#947c22,126C;#f2e39a,1205C;#f2dc85,1215C;#f2c95a,1225C;#f0b430,1235C;#b7912b,1245C;#9f8330,1255C;#7d682c,1265C;#ecde7a,127C;#ead55e,128C;#e9ce52,129C;#dea900,130C;#bd8d00,131C;#957616,132C;#675823,133C;#f0d37b,134C;#f0c459,135C;#efba4a,136C;#eca018,137C;#c77900,138C;#a06e13,139C;#6c5423,140C;#f0d08c,1345C;#f0c578,1355C;#efb459,1365C;#ec9e3c,1375C;#be7512,1385C;#8d6222,1395C;#624a25,1405C;#e5ca6b,141C;#e1bc52,142C;#ddae3f,143C;#d1820e,144C;#b57613,145C;#8e641c,146C;#695a31,147C;#efcd94,148C;#eec283,149C;#eca759,150C;#e47807,151C;#c87015,152C;#a7651f,153C;#855320,154C;#f0af73,1485C;#ee903d,1495C;#e76d00,1505C;#e05800,Orange_021C;#ac4d00,1525C;#7f441a,1535C;#4d3120,1545C;#e7d5a6,155C;#e0c084,156C;#d59850,157C;#ca722d,158C;#af5c1f,159C;#8c5221,160C;#593e23,161C;#efbf9c,1555C;#eda97c,1565C;#e9874e,1575C;#e26d2c,1585C;#bc5d24,1595C;#8f512a,1605C;#774526,1615C;#efc0a2,162C;#ec9d73,163C;#e87e4a,164C;#e16325,165C;#c45317,166C;#a65023,167C;#623520,168C;#eca28a,1625C;#ea8e71,1635C;#e46d46,1645C;#db5020,1655C;#c0491e,1665C;#8e4126,1675C;#733b27,1685C;#eeb5ad,169C;#e98a7c,170C;#e15c41,171C;#d84824,172C;#b74a30,173C;#863e2c,174C;#643b31,175C;#edaeb6,176C;#e7808b,177C;#e0585f,178C;#d6423d,Warm_Red_C;#bf3b35,179C;#a33936,180C;#6b2d2b,181C;#e89dad,1765C;#e4899c,1775C;#d44559,1785C;#ca2c3b,1788C;#b0252f,1795C;#922b32,1805C;#68282b,1815C;#e7b0bf,1767C;#db637e,1777C;#d4415a,1787C;#cc2d3b,Red_032C;#a82a31,1797C;#89333b,1807C;#543335,1817C;#e9bac8,182C;#e291a9,183C;#d55978,184C;#c00036,185C;#a91433,186C;#8f1f33,187C;#672934,188C;#e1a2b8,189C;#d87798,190C;#cd456f,191C;#c01549,192C;#a01a3f,193C;#812840,194C;#693543,195C;#e4b9cc,1895C;#de9db9,1905C;#cc5181,1915C;#bc0c51,1925C;#a60345,1935C;#8d1841,1945C;#79213e,1955C;#dfc4c9,196C;#d299a8,197C;#bf4f68,198C;#b1173c,199C;#9d1936,200C;#832335,201C;#712935,202C;#d8adc4,203C;#cb7da3,204C;#be4a7c,205C;#ad0044,206C;#8e0a41,207C;#752846,208C;#602b3f,209C;#e4a0c8,210C;#dc79b1,211C;#d14e95,212C;#c21d73,213C;#ac0060,214C;#8f1856,215C;#6c2849,216C;#ddbbd6,217C;#c86eaf,218C;#b82481,219C;#ab005c,Rubine_Red_C;#8a0050,220C;#7b004b,221C;#5c2042,222C;#da91ca,223C;#cf67b5,224C;#c02996,225C;#af006f,226C;#8f005f,227C;#6f0051,228C;#5a2546,229C;#e2a2d2,230C;#d874c2,231C;#ca3ea9,232C;#c01899,Rhodamine_Red_C;#a70080,233C;#890069,234C;#720056,235C;#e1acdb,236C;#d789cf,237C;#c85bba,238C;#bc3caa,239C;#a42792,240C;#8b207b,241C;#672658,242C;#e3c0df,2365C;#c86fc5,2375C;#b431ac,2385C;#a60099,2395C;#8b0080,2405C;#7b0072,2415C;#6b0061,2425C;#debbe0,243C;#d39dd8,244C;#c880cf,245C;#a622a8,246C;#970d98,247C;#851e85,248C;#662c62,249C;#dcbfe1,250C;#cb9cda,251C;#b266c9,252C;#9d37b6,Purple_C;#8f2ba5,253C;#813094,254C;#613069,255C;#d5c5dd,256C;#c1a7cf,257C;#83519a,258C;#602b78,259C;#562a65,260C;#502959,261C;#4b2c4f,262C;#c8a9df,2562C;#b58ad6,2572C;#904ec0,2582C;#7b28ae,2592C;#6a1796,2602C;#5e2479,2612C;#543160,2622C;#bea3d4,2563C;#a581c4,2573C;#8d60b1,2583C;#703a97,2593C;#602a82,2603C;#572373,2613C;#512567,2623C;#b29bd3,2567C;#997cc5,2577C;#754cac,2587C;#4c0e88,2597C;#451378,2607C;#41166d,2617C;#3c1b5d,2627C;#d7cee7,263C;#bbaee2,264C;#836bcc,265C;#5c3cb3,266C;#4a2894,267C;#48307b,268C;#463368,269C;#c1b8e1,2635C;#a597d8,2645C;#8a77cd,2655C;#745cc2,2665C;#42119c,Violet_C;#34007f,2685C;#312254,2695C;#b3b4da,270C;#9091cb,271C;#7577bc,272C;#261d70,273C;#221b5c,274C;#221d55,275C;#25223f,276C;#a3a2dc,2705C;#8683d2,2715C;#615ac0,2725C;#24008d,2735C;#220075,2745C;#211068,2755C;#1f1651,2765C;#cbd0e6,2706C;#a0a9df,2716C;#525cc0,2726C;#2a2aa4,2736C;#27298e,2746C;#21256b,2756C;#212654,2766C;#c9d6ea,2707C;#b0c4e9,2717C;#567dd7,2727C;#001ea3,Blue_072C;#0f2977,2747C;#182b62,2757C;#242f49,2767C;#b8c7e5,2708C;#6884d3,2718C;#304fb8,2728C;#132192,2738C;#162571,2748C;#1b2763,2758C;#1c254b,2768C;#b3c9e4,277C;#9bb9e3,278C;#6690d8,279C;#002891,Reflex_Blue_C;#002b74,280C;#0d2b61,281C;#152648,282C;#a5c4e8,283C;#80abe1,284C;#3473cb,285C;#003ca2,286C;#003689,287C;#003074,288C;#102745,289C;#c9dde9,290C;#adcde9,291C;#7fafe2,292C;#0048a9,293C;#003775,294C;#00325e,295C;#162432,296C;#a1c9e5,2905C;#7eb4e1,2915C;#4e96d8,2925C;#005bb6,2935C;#00559b,2945C;#003e68,2955C;#142f46,2965C;#90c6e5,297C;#71b6e2,298C;#499fdb,299C;#0065b9,300C;#005390,301C;#004364,302C;#173447,303C;#b4dae6,2975C;#84c4e6,2985C;#4aa8de,2995C;#0079c5,3005C;#00669e,3015C;#085270,3025C;#1b4353,3035C;#b4dde8,304C;#8dcee7,305C;#57b8e2,306C;#0086ca,Process_Blue_C;#0074ad,307C;#005b7f,308C;#183f4d,309C;#94d3e2,310C;#70c4dc,311C;#29abce,312C;#0096c0,313C;#0082a6,314C;#006981,315C;#204b54,316C;#95d5df,3105C;#6bc4d6,3115C;#33afc7,3125C;#0093b1,3135C;#007b90,3145C;#006777,3155C;#0f515c,3165C;#c7e7e5,317C;#a9dee1,318C;#7ecdd4,319C;#0099a4,320C;#008a93,321C;#0e7379,322C;#216265,323C;#b0dcd8,324C;#8ccdc8,325C;#50b1a8,326C;#008671,327C;#007264,328C;#01675b,329C;#2f5752,330C;#99dbd3,3242C;#84d4cc,3252C;#4abfb4,3262C;#00a398,3272C;#00867c,3282C;#215d56,3292C;#2a4f48,3302C;#a1dfd2,3245C;#81d5c5,3255C;#57c6b1,3265C;#23b099,3275C;#2c9481,3285C;#267a6a,3295C;#2d4f46,3305C;#9cd4c4,3248C;#81c8b5,3258C;#4bae92,3268C;#299a75,3278C;#228468,3288C;#276d57,3298C;#26463c,3308C;#c1e8da,331C;#b7e6d7,332C;#88dac2,333C;#00ac84,Green_C;#1b9977,334C;#2d7e65,335C;#2e6753,336C;#aedbc5,337C;#96d1b6,338C;#59b489,339C;#2a9762,340C;#337c5a,341C;#326a4f,342C;#325745,343C;#a3e1c1,3375C;#8ad9b0,3385C;#57c68d,3395C;#2dad69,3405C;#307a51,3415C;#316646,3425C;#2a4935,3435C;#badebe,344C;#a8d7af,345C;#93cd9d,346C;#3d9a4f,347C;#368448,348C;#386941,349C;#39503a,350C;#bae5bd,351C;#aae1b0,352C;#a0dea7,353C;#45ae4a,354C;#3e9944,355C;#3a783c,356C;#3d593b,357C;#b8dc95,358C;#b2d98e,359C;#85c159,360C;#6cb042,361C;#659b40,362C;#5b8839,363C;#567638,364C;#d0e49d,365C;#d0e49d,366C;#b4d76f,367C;#87bd3c,368C;#74a42f,369C;#6d8e31,370C;#5a6833,371C;#ddeb9f,372C;#d7ea8e,373C;#cde777,374C;#a7d32d,375C;#8eb61a,376C;#7d9423,377C;#596027,378C;#e2e876,379C;#d9e252,380C;#cddc34,381C;#c4d52a,382C;#a4ac14,383C;#8e9216,384C;#706e2c,385C;#e8eb77,386C;#e1e748,387C;#dbe437,388C;#d0db00,389C;#b7bd00,390C;#9a9813,391C;#7d791a,392C;#edec88,393C;#eae955,394C;#e6e62f,395C;#dede00,396C;#beb900,397C;#aaa300,398C;#979000,399C;#f1ed79,3935C;#eee732,3945C;#e6de00,3955C;#e0d600,3965C;#aea200,3975C;#918514,3985C;#665e22,3995C;#c8c6be,400C;#b3b0a8,401C;#a6a29a,402C;#8f8a81,403C;#746e66,404C;#5d5850,405C;#2d2b28,Black_C;#cac5bf,406C;#b1aba4,407C;#9d9690,408C;#88807b,409C;#726965,410C;#5a504d,411C;#342f2e,412C;#c4c5bb,413C;#afafa5,414C;#98988e,415C;#828379,416C;#6d6e65,417C;#585950,418C;#222321,419C;#cdceca,420C;#b4b5b1,421C;#a2a3a1,422C;#8d8f8e,423C;#6c6f6f,424C;#585a5c,425C;#202224,426C;#d1d3d2,427C;#c3c6c6,428C;#a6aaad,429C;#83898e,430C;#626970,431C;#3d444b,432C;#23282d,433C;#cbc2c2,434C;#bcb1b4,435C;#ab9ea2,436C;#79686e,437C;#4d3e42,438C;#403435,439C;#322b2a,440C;#bec4c0,441C;#abb1b0,442C;#959c9c,443C;#777e80,444C;#505458,445C;#434647,446C;#393a38,447C;#dfddd7,Warm_Gray_1C;#d3d1c9,Warm_Gray_2C;#c4c0b9,Warm_Gray_3C;#b4afa8,Warm_Gray_4C;#aaa59e,Warm_Gray_5C;#a29b94,Warm_Gray_6C;#948d86,Warm_Gray_7C;#878078,Warm_Gray_8C;#7f776f,Warm_Gray_9C;#726a62,Warm_Gray_10C;#645c55,Warm_Gray_11C;#e0e0dc,Cool_Gray_1C;#d4d5d1,Cool_Gray_2C;#c8c9c7,Cool_Gray_3C;#bbbcba,Cool_Gray_4C;#b1b2b1,Cool_Gray_5C;#acadad,Cool_Gray_6C;#999a9a,Cool_Gray_7C;#8b8c8d,Cool_Gray_8C;#747577,Cool_Gray_9C;#616365,Cool_Gray_10C;#4e5053,Cool_Gray_11C;#3d392a,Black_2C;#2c302b,Black_3C;#383129,Black_4C;#413538,Black_5C;#1b2228,Black_6C;#393837,Black_7C;#4a4631,448C;#4c492c,449C;#4f4d2c,450C;#99986f,451C;#b2b28d,452C;#c1c1a0,453C;#cfcfb4,454C;#574b27,4485C;#7b6d34,4495C;#928548,4505C;#afa66f,4515C;#c2ba8a,4525C;#cec89e,4535C;#d9d5b2,4545C;#615626,455C;#93832a,456C;#a89223,457C;#d7cb6f,458C;#ddd37d,459C;#e3dc92,460C;#e7e3aa,461C;#54472e,462C;#644e2a,463C;#785c2e,464C;#ab9760,465C;#c0b280,466C;#ccc196,467C;#d9d2af,468C;#4a2f22,4625C;#855e3e,4635C;#a17c5d,4645C;#b19072,4655C;#c3a78d,4665C;#d3bfa8,4675C;#dbcbb7,4685C;#573824,469C;#8b532b,470C;#9d5524,471C;#d19a6e,472C;#e2bd9c,473C;#e4c3a2,474C;#e7ccaf,475C;#4b3029,4695C;#6c4b3d,4705C;#8b6d5d,4715C;#a18776,4725C;#b7a393,4735C;#c5b5a6,4745C;#d2c6b9,4755C;#47362c,476C;#55382b,477C;#653f2e,478C;#9d8066,479C;#bfab95,480C;#ccbca9,481C;#d4c7b4,482C;#5c362c,483C;#843528,484C;#b72f24,485C;#d18d77,486C;#d8a58f,487C;#debca9,488C;#e2c8b7,489C;#522f33,490C;#69343a,491C;#78373f,492C;#c28294,493C;#d39ead,494C;#dfb6c2,495C;#e4c3cb,496C;#483131,497C;#5e3938,498C;#683d3a,499C;#b5848d,500C;#cca5ad,501C;#dbbdc3,502C;#e1c9cc,503C;#40282a,4975C;#764d55,4985C;#8f666f,4995C;#a8838b,5005C;#c3a6ab,5015C;#d1babc,5025C;#dbc9ca,5035C;#513037,504C;#61303f,505C;#72364a,506C;#c38fa6,507C;#d4aabc,508C;#dab6c5,509C;#dfbfcb,510C;#542b4f,511C;#67266d,512C;#7a2a89,513C;#c089c5,514C;#d3a9d5,515C;#dbbadb,516C;#e0c6de,517C;#462d43,5115C;#614361,5125C;#7b5f7d,5135C;#967e97,5145C;#bbaabc,5155C;#cfc4cf,5165C;#d4cad3,5175C;#49354d,518C;#51345e,519C;#5f3d75,520C;#a189b5,521C;#b6a3c7,522C;#c5b6d1,523C;#d2c7da,524C;#463544,5185C;#5c4659,5195C;#7f687b,5205C;#a693a1,5215C;#bdadb7,5225C;#ccbfc6,5235C;#dbd3d6,5245C;#4c325e,525C;#5a3183,526C;#6432a1,527C;#a07fcc,528C;#baa1d9,529C;#cbb6df,530C;#d5c4e2,531C;#2b2945,5255C;#413d64,5265C;#555279,5275C;#8482a1,5285C;#adacc1,5295C;#bebecd,5305C;#d1d2da,5315C;#2e303b,532C;#2b354d,533C;#324168,534C;#95a0bb,535C;#a7b2c6,536C;#c0c8d4,537C;#cfd4db,538C;#122e43,539C;#003659,540C;#004170,541C;#789ec5,542C;#a9c2dc,543C;#bed1e2,544C;#c9d8e3,545C;#172632,5395C;#52697c,5405C;#677e90,5415C;#8599a8,5425C;#abbac4,5435C;#bcc8cf,5445C;#c9d2d6,5455C;#1d363d,546C;#0b3c47,547C;#004451,548C;#739aac,549C;#99b7c4,550C;#abc4cd,551C;#c4d5d9,552C;#1a3139,5463C;#3d656f,5473C;#6c8f97,5483C;#90adb3,5493C;#a4bdc1,5503C;#bccfd0,5513C;#c8d7d6,5523C;#2a3935,5467C;#4a5d57,5477C;#6a7c77,5487C;#8e9d98,5497C;#abb7b2,5507C;#bcc6c2,5517C;#cbd2cf,5527C;#314536,553C;#3b5d45,554C;#436c4c,555C;#81a389,556C;#9db8a3,557C;#b2c8b6,558C;#c2d3c3,559C;#2c3a34,5535C;#556b61,5545C;#74897e,5555C;#92a59b,5565C;#a7b7ae,5575C;#bdcac2,5585C;#ced8d0,5595C;#32443d,560C;#395e55,561C;#45776e,562C;#90bab1,563C;#a8cbc2,564C;#bfd9d2,565C;#d0e2db,566C;#2c392e,5605C;#606f5e,5615C;#778574,5625C;#9aa697,5635C;#b0baac,5645C;#bdc5b8,5655C;#c9cfc4,5665C;#30463e,567C;#39685c,568C;#418476,569C;#94c8bc,570C;#b2d9cf,571C;#bfdfd6,572C;#c9e3d9,573C;#49522d,574C;#607638,575C;#769143,576C;#b2c688,577C;#bece97,578C;#c5d4a0,579C;#cfdaae,580C;#454c2f,5743C;#5e633a,5753C;#707649,5763C;#91966d,5773C;#a9ae8a,5783C;#b9bd9c,5793C;#cbceb3,5803C;#424622,5747C;#6c6f32,5757C;#8a8e50,5767C;#a3a76e,5777C;#bec193,5787C;#c9cca3,5797C;#d7d9ba,5807C;#5a5722,581C;#86871e,582C;#aab221,583C;#cfd457,584C;#dbdf78,585C;#dfe287,586C;#e3e599,587C;#4b4823,5815C;#807c3c,5825C;#9d9a5d,5835C;#aca96f,5845C;#c2c090,5855C;#cccba0,5865C;#d4d3ae,5875C;#eceaac,600C;#ecea9e,601C;#ebe890,602C;#e9e260,603C;#e6dd42,604C;#dbcc00,605C;#ccb800,606C;#e9e8b2,607C;#e7e5a1,608C;#e4e08c,609C;#dcd668,610C;#d0c745,611C;#bbae13,612C;#a99a00,613C;#dfddaf,614C;#d8d49e,615C;#d0cc8d,616C;#c3bd74,617C;#aaa24d,618C;#968e39,619C;#837a2a,620C;#d4ded6,621C;#bdcdc3,622C;#a5baaf,623C;#87a194,624C;#678475,625C;#415e51,626C;#263830,627C;#cae1e4,628C;#b1d7df,629C;#9ccbda,630C;#71b5cc,631C;#439ab9,632C;#0d7da0,633C;#00678d,634C;#bbdde5,635C;#a7d6e5,636C;#81c5e0,637C;#4eaed5,638C;#1798c9,639C;#0081b8,640C;#0073ad,641C;#d5dde3,642C;#c7d2de,643C;#c7d2de,644C;#7f98ba,645C;#6481a8,646C;#365785,647C;#0d2d57,648C;#d9dfe5,649C;#c7d1de,650C;#a1b1cb,651C;#798fb4,652C;#395787,653C;#15305e,654C;#12264e,655C;#dbe2ea,656C;#c9d5e7,657C;#adc0e1,658C;#7c99d0,659C;#486eb7,660C;#00388e,661C;#002274,662C;#dedbe2,663C;#dad7e1,664C;#c2bcd0,665C;#9b91b1,666C;#72658f,667C;#5c4e7a,668C;#3f315c,669C;#e2d2e0,670C;#dbbfd6,671C;#cea2c6,672C;#c287b5,673C;#ae5e98,674C;#942d71,675C;#800953,676C;#decfd9,677C;#d8c6d5,678C;#d3bccf,679C;#ba95b2,680C;#a37398,681C;#844a73,682C;#682d52,683C;#dac3d0,684C;#d3b6c8,685C;#c7a3bc,686C;#b285a5,687C;#9f6a90,688C;#7e416b,689C;#572446,690C;#e2d2d6,691C;#d9c0c6,692C;#cba9b1,693C;#b78b97,694C;#a16e7c,695C;#7d4653,696C;#733e48,697C;#e8d4d9,698C;#e3c3cc,699C;#daa9b7,700C;#cd889c,701C;#b76077,702C;#9c3b4f,703C;#8b2f3b,704C;#ebdade,705C;#ebdade,706C;#e3b1bf,707C;#da8ba0,708C;#cf6680,709C;#c14760,710C;#b33345,711C;#eccba6,712C;#ecc297,713C;#e8ad75,714C;#df9148,715C;#d37920,716C;#bf5e00,717C;#af4f00,718C;#e4ceb3,719C;#debe9b,720C;#d4ac82,721C;#bc8853,722C;#a86f36,723C;#844b18,724C;#713f18,725C;#ddcab1,726C;#d2b89a,727C;#c7a886,728C;#af8960,729C;#996f43,730C;#663f1d,731C;#56361e,732C;#ece3bb,7401C;#e7dc9e,7402C;#e1cd7d,7403C;#ead236,7404C;#e0c000,7405C;#ddb500,7406C;#bd9a50,7407C;#e1ad08,7408C;#dead28,7409C;#e6a473,7410C;#d1a15d,7411C;#b97939,7412C;#be7a2f,7413C;#a36227,7414C;#d8b9a3,7415C;#c6684d,7416C;#c05138,7417C;#a94e52,7418C;#93495a,7419C;#832541,7420C;#521e30,7421C;#e8d3d9,7422C;#c56688,7423C;#bc3f7b,7424C;#9e2d59,7425C;#921f49,7426C;#801d31,7427C;#603142,7428C;#dac3cf,7429C;#cfaec0,7430C;#ba88a3,7431C;#a16181,7432C;#934267,7433C;#823455,7434C;#70294e,7435C;#e2d5e2,7436C;#c4b0cf,7437C;#c199d2,7438C;#a88eb8,7439C;#9679a6,7440C;#8960bd,7441C;#7539b5,7442C;#d7d8e4,7443C;#b6b9d9,7444C;#a3a0c3,7445C;#8d8bc8,7446C;#564677,7447C;#463951,7448C;#3a2b3b,7449C;#bcc1d9,7450C;#91a7dd,7451C;#8591d8,7452C;#87a2d6,7453C;#7391b2,7454C;#4c61ab,7455C;#6a72b3,7456C;#d0e2e8,7457C;#88b3c9,7458C;#5c94b1,7459C;#0088c1,7460C;#3382bb,7461C;#2a5b88,7462C;#193450,7463C;#b0d5d1,7464C;#76c3b4,7465C;#30b1bc,7466C;#00a7b2,7467C;#327497,7468C;#135d81,7469C;#215e6d,7470C;#a1ddd6,7471C;#7fbab5,7472C;#5c9b8a,7473C;#2e7985,7474C;#5c7e7f,7475C;#2d5157,7476C;#37515f,7477C;#b7e5c2,7478C;#7cd082,7479C;#5dc377,7480C;#4eb855,7481C;#43a357,7482C;#3f5e3c,7483C;#315940,7484C;#dce4cd,7485C;#cde4a6,7486C;#b0df77,7487C;#98d55c,7488C;#86ad5c,7489C;#789544,7490C;#788541,7491C;#c9d18d,7492C;#bcc597,7493C;#a3b190,7494C;#8b9541,7495C;#707e24,7496C;#726e55,7497C;#515731,7498C;#ebe7c5,7499C;#ded7b7,7500C;#d7ccac,7501C;#cdbe97,7502C;#a39d72,7503C;#8a785d,7504C;#7a6347,7505C;#e8ddbb,7506C;#edd5a6,7507C;#d9bf8d,7508C;#ccaa70,7509C;#b88e49,7510C;#a16f25,7511C;#905c1e,7512C;#dbbead,7513C;#caa790,7514C;#ba9173,7515C;#895533,7516C;#764426,7517C;#665149,7518C;#5f5348,7519C;#dfc2b6,7520C;#bca593,7521C;#a5735d,7522C;#9a675f,7523C;#935a4e,7524C;#8f6e53,7525C;#7c3e25,7526C;#d8d6cb,7527C;#c6bfb5,7528C;#b9b0a5,7529C;#a59b8e,7530C;#7f7364,7531C;#625648,7532C;#483f34,7533C;#d5d2c6,7534C;#bbb7a6,7535C;#a7a28d,7536C;#aeb2aa,7537C;#9ca098,7538C;#989996,7539C;#5f6166,7540C;#e1e5e5,7541C;#b1bfc4,7542C;#a5adb4,7543C;#8c959e,7544C;#57626e,7545C;#414c58,7546C;#242c35,7547C;#46aed5,801C;#8eda4d,802C;#ffe73c,803C;#ff9742,804C;#fb545b,805C;#e1179f,806C;#b800ae,807C;#49b08f,808C;#e1e032,809C;#ffc532,810C;#ff6643,811C;#e72a6a,812C;#c70ea5,813C;#745bc6,814C;#8a7b4f,871C;#85744d,872C;#8c7650,873C;#846e4f,874C;#82674e,875C;#88664e,876C;#858789,877C;#81796e,8003C;#8b766b,8021C;#8b707b,8062C;#817789,8100C;#6b7f8f,8201C;#798a88,8281C;#7e8873,8321C";
            colorSheetSet.Sheets.Add(createColorSheetWithColorNameList("潘通(PANTONE)-Coated", PANTONE_C));

            string threeColors = "#ff0000,RED;#00FF00,GREEN;#0000FF,BLUE";
            colorSheetSet.Sheets.Add(createColorSheetWithColorNameList("THREE-COLORS", threeColors));
            string sevenColors = "#ff0000,赤;#ffa500,橙;#ffff00,黄;#00ff00,绿;#007fff,青;#0000ff,蓝;#8b00ff,紫;";
            colorSheetSet.Sheets.Add(createColorSheetWithColorNameList("SEVEN-COLORS", sevenColors));

            string fileName = "colorsheetset.txt";
            StreamWriter sw = new StreamWriter(fileName, false, Encoding.UTF8);
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(colorSheetSet, Newtonsoft.Json.Formatting.Indented);
            sw.WriteLine(json);
            sw.Close();
        }

        ColorSheet createColorSheetWithSimpleColorList(string name, string colorList)
        {
            char[] spliter = { ',' };
            var colors = colorList.Split(spliter);

            ColorSheet sheet = new ColorSheet();
            sheet.Name = name;
            sheet.Colors = new List<ColorModel>();
            foreach (var c in colors)
            {
                ColorModel cm = new ColorModel();
                cm.Name = c;
                cm.Color = c;
                cm.ColorBrush = GetBrushFromString(c);
                sheet.Colors.Add(cm);
            }
            return sheet;
        }

        ColorSheet createColorSheetWithColorNameList(string name, string colorList)
        {
            char[] spliter1 = { ',' };
            char[] spliter2 = { ';' };
            var colors = colorList.Split(spliter2);

            ColorSheet sheet = new ColorSheet();
            sheet.Name = name;
            sheet.Colors = new List<ColorModel>();
            foreach (var c in colors)
            {
                string[] cnpair = c.Split(spliter1);
                ColorModel cm = new ColorModel();
                if (cnpair.Length != 2)
                    continue;
                cm.Name = cnpair[1];
                cm.Color = cnpair[0];
                cm.ColorBrush = GetBrushFromString(cm.Color);
                sheet.Colors.Add(cm);
            }
            return sheet;
        }

        public static Color StringToColor(string color)
        {
            if(string.IsNullOrEmpty(color))
                return Colors.White;
            if (color[0] == '#')
                color = color.Substring(1);
            if(color.Length == 6)
            {
                return Color.FromArgb(
                    255,
                byte.Parse(color.Substring(0, 2), System.Globalization.NumberStyles.HexNumber),
                byte.Parse(color.Substring(2, 2), System.Globalization.NumberStyles.HexNumber),
                byte.Parse(color.Substring(4, 2), System.Globalization.NumberStyles.HexNumber));
            }
            else if(color.Length == 8)
            {
                return Color.FromArgb(
                byte.Parse(color.Substring(6, 2), System.Globalization.NumberStyles.HexNumber),
                byte.Parse(color.Substring(0, 2), System.Globalization.NumberStyles.HexNumber),
                byte.Parse(color.Substring(2, 2), System.Globalization.NumberStyles.HexNumber),
                byte.Parse(color.Substring(4, 2), System.Globalization.NumberStyles.HexNumber));
            }
            return Colors.White;
        }

        public static string ColorToString(Color c)
        {
            return string.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", c.R, c.G, c.B, c.A);
        }

        public Brush GetBrushFromString(string color)
        {
            return new SolidColorBrush(StringToColor(color));
        }

        public string GetFaceName(int index)
        {
            if(faceDefines != null && faceDefines.Faces != null && faceDefines.Faces.Count > index)
                return faceDefines.Faces[index].Name;
            return "";
        }
        void LoadFaceDefines()
        {
            faceDefines = new SlotFaceDefineSet();
            faceDefines.Faces = new List<SlotFaceDefine>();
            faceDefines.Faces.Add(new SlotFaceDefine { Index = 0, Name = "0 +X 前面", Color = GetBrushFromString("f0ccd3"), Description = "" });
            faceDefines.Faces.Add(new SlotFaceDefine { Index = 1, Name = "1 -X 后面", Color = GetBrushFromString("b2cf87"), Description = "" });
            faceDefines.Faces.Add(new SlotFaceDefine { Index = 2, Name = "2 +Y 右面", Color = GetBrushFromString("f6e497"), Description = "" });
            faceDefines.Faces.Add(new SlotFaceDefine { Index = 3, Name = "3 -Y 左面", Color = GetBrushFromString("82cbab"), Description = "" });
            faceDefines.Faces.Add(new SlotFaceDefine { Index = 4, Name = "4 +Z 上面", Color = GetBrushFromString("c7aac3"), Description = "" });
            faceDefines.Faces.Add(new SlotFaceDefine { Index = 5, Name = "5 -Z 下面", Color = GetBrushFromString("63bbd0"), Description = "" });
        }
        void UseDefaultSlotDefines()
        {
            SlotPredefines = new SlotPredefines();
            SlotPredefines.Slots = new List<SlotPredefineModel>();
            SlotPredefineModel slot1 = new SlotPredefineModel() { Type = "slot", Depth = 5, Diameter = 8, ScrewType = "ISO 8x5" };
            SlotPredefineModel slot2 = new SlotPredefineModel() { Type = "slot", Depth = 12, Diameter = 8, ScrewType = "ISO 8x12" };
            SlotPredefineModel slot3 = new SlotPredefineModel() { Type = "slot", Depth = 6, Diameter = 4, ScrewType = "ISO 4x6" };
            SlotPredefines.Slots.Add(slot1);
            SlotPredefines.Slots.Add(slot2);
            SlotPredefines.Slots.Add(slot3);
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(SlotPredefines, Newtonsoft.Json.Formatting.Indented);

            string fileName = "slotpredefines.txt";
            StreamWriter sw = new StreamWriter(fileName, false, Encoding.UTF8);
            sw.WriteLine(json);
            sw.Close();
        }

        public async Task<int> LoadAllCategories()
        {
            rootCategories.Clear();
            categoryMap.Clear();
            categoryNameToIdMap.Clear();
            var result = await api.GetAsync<CategoryAllModel>("/category/all");
            if (result.IsSuccess)
            {
                if (result.Content != null && result.Content.categories != null)
                {
                    rootCategories = result.Content.categories;
                    foreach (var root in rootCategories)
                    {
                        registerCategory(root);
                    }
                }
            }

            CategoryUpdated?.Invoke();

            return result.StatusCode;
        }

        void registerCategory(CategoryModel cate)
        {
            categoryMap[cate.id] = cate;
            categoryNameToIdMap[cate.name] = cate.id;

            foreach (var item in cate.children)
            {
                registerCategory(item);
            }
        }

        public string GetCategoryIdByName(string name)
        {
            string id;
            categoryNameToIdMap.TryGetValue(name, out id);
            return id;
        }
        public CategoryModel GetCategoryById(string id)
        {
            CategoryModel re = null;
            categoryMap.TryGetValue(id, out re);
            return re;
        }

        public async Task<string> UploadFileAndGetId(string localpath, string md5 = "")
        {
            if (string.IsNullOrEmpty(localpath) || File.Exists(localpath) == false)
                return "";

            if (string.IsNullOrEmpty(md5))
            {
                md5 = Md5Helper.Md5File(localpath);
            }

            var checkres = await api.GetAsync<FileModel>("/files/" + md5);
            if (checkres.IsSuccess && checkres.StatusCode == 200 && checkres.Content != null)
            {
                return checkres.Content.id;
            }

            var res = await api.UploadFile("/files/upload", localpath);
            if (res?.IsSuccess == false || string.IsNullOrEmpty(res.Content))
                return "";

            var file = Newtonsoft.Json.JsonConvert.DeserializeObject<FileModel>(res.Content);
            return file == null ? "" : file.id;
        }
        public async Task<FileModel> UploadFile(string localpath, string md5 = "")
        {
            if (string.IsNullOrEmpty(localpath) || File.Exists(localpath) == false)
                return null;

            if(string.IsNullOrEmpty(md5))
            {
                md5 = Md5Helper.Md5File(localpath);
            }

            var checkres = await api.GetAsync<FileModel>("/files/" + md5);
            if(checkres.IsSuccess && checkres.StatusCode == 200 && checkres.Content != null)
            {
                return checkres.Content;
            }

            var res = await api.UploadFile("/files/upload", localpath);
            if (res?.IsSuccess == false || string.IsNullOrEmpty(res.Content))
                return null;

            return Newtonsoft.Json.JsonConvert.DeserializeObject<FileModel>(res.Content);
        }
    }
}
