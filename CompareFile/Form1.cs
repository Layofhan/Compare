using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CompareFile
{
    public partial class Form1 : Form
    {
        private SyncListBoxes _SyncListBoxes = null;

        //存放两个文件每一行的内容
        private static List<Node> compareOneList = new List<Node>();
        private static List<Node> compareTwoList = new List<Node>();
        //这几个是用于数据预处理的时候使用的变量，存放的是原始的文件内容
        //这几个变量可以考虑在空间上进行优化
        private static string[] linesOne;
        private static string[] linesTwo;
        private static int[] sameArray;//这个是保存两个文件内容相同行的行号集合

        //存放文件差异点的位置
        private static List<DifferentFlag> differentList = new List<DifferentFlag>();
        private static int difRowFlag = 0;//当前差异行定位标记

        //两个字符串差异点的位置，用于富文本框文字渲染时使用
        private static List<int> difOne = new List<int>();
        private static List<int> difTwo = new List<int>();


        private static bool comapreMode = false;
        public Form1()
        {
            InitializeComponent();
            this.Load += Form1_Load;
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            this._SyncListBoxes = new SyncListBoxes(this.listBox1, this.listBox2);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Init();//内存对象初始化
            label3.Text = "正在进行比对，请稍等......";
            compareAlgorithm(txtFileOne.Text,txtFileTwo.Text);

            compareOneList.OrderBy(a => a.index);
            compareTwoList.OrderBy(a => a.index);
            differentList.OrderBy(a => a.row);
            //展示数据
            foreach (Node node in compareOneList)
            {
                listBox1.Items.Add(node.text);
            }
            foreach (Node node in compareTwoList)
            {
                listBox2.Items.Add(node.text);
            }

            label3.Text = "比对成功!!";
        }

        #region 上一个 下一个差异点的跳转按钮逻辑
        private void btnNextDifferent_Click(object sender, EventArgs e)
        {
            if (differentList.Count == 0) return;//没有则代表完全一致
            DifferentFlag temp = differentList.FirstOrDefault(a => a.row > difRowFlag);
            if (temp != null)
            {
                listBox1.SelectedIndex = temp.row;
                listBox2.SelectedIndex = temp.row;
                difRowFlag = temp.row;
            }
        }

        private void btnPreviousDifferent_Click(object sender, EventArgs e)
        {
            if (differentList.Count == 0) return;//没有则代表完全一致
            DifferentFlag temp = differentList.FindLast(a => a.row < difRowFlag);
            if (temp != null)
            {
                listBox1.SelectedIndex = temp.row;
                listBox2.SelectedIndex = temp.row;
                //difRowFlag = temp.row;
            }
        }
        #endregion

        //准备比对数据
        public static void compareAlgorithm(string p_FilePathOne,string p_FilePathTwo)
        {
            //使用gbk格式来进行文件的读取
            FileStream fileOne = new FileStream(p_FilePathOne, FileMode.Open);
            FileStream fileTwo = new FileStream(p_FilePathTwo, FileMode.Open);
            StreamReader srOne = new StreamReader(fileOne, Encoding.GetEncoding("GBK"));
            StreamReader srTwo = new StreamReader(fileTwo, Encoding.GetEncoding("GBK"));
            
            //有两种匹配模式可以进行选择
            if (comapreMode)//这个匹配模式下 可以判断出两个文件的删除行、新增行、修改行和
            {
                #region 用于解决两个文件码行的问题
                //获取全部内容
                string textOne = srOne.ReadToEnd();
                string textTwo = srTwo.ReadToEnd();
                
                //hash表，key为每行的内容，value为hash值为一个递增的值
                Hashtable h = new Hashtable(textOne.Length + textTwo.Length);

                textOne = textOne.Replace("\r", "");
                linesOne = textOne.Split('\n');
                textTwo = textTwo.Replace("\r", "");
                linesTwo = textTwo.Split('\n');
                //利用hashtable 进行hash值比对
                int[] arrayOne = DiffCodes(textOne, h, linesOne);
                int[] arrayTwo = DiffCodes(textTwo, h, linesTwo);

                //预处理数据
                //【这一步的作用就是先将两个文件的内容把码行的问题解决 码的行用空行来代替，然后把预处理好的内容放到compareOneList、compareTwoList两个变量中，下面再直接进行比对差异的行】
                //这里事实上有很大的一个时间和空间上的优化空间
                dealText(arrayOne, arrayTwo, linesOne, linesTwo);
                //return;
                #endregion
                #region 重写匹配
                Node[] compareOneArray = compareOneList.ToArray();
                Node[] compareTwoArray = compareTwoList.ToArray();
                int i = 0;
                while (i < compareOneArray.Length && i < compareTwoArray.Length)//理论上来说这两个长度是一致的
                {
                    string oneStrTemp = compareOneArray[i].text;
                    string twoStrTemp = compareTwoArray[i].text;

                    //具体的匹配逻辑
                    if (oneStrTemp != twoStrTemp)
                    {
                        DifferentFlag diffTemp = compareStrByChar(oneStrTemp, twoStrTemp);
                        diffTemp.row = i;
                        differentList.Add(diffTemp);
                    }
                    //end
                    i++;
                }
                //这里还要补充可能其中一个没有遍历完成的
                while (i < compareOneArray.Length)
                {
                    DifferentFlag diffTemp = new DifferentFlag();
                    diffTemp.row = i;
                    diffTemp.leftcolA = 0;
                    diffTemp.rightColA = compareOneArray[i].text.Length;

                    differentList.Add(diffTemp);
                    i++;
                }
                while (i < compareTwoArray.Length)
                {
                    DifferentFlag diffTemp = new DifferentFlag();
                    diffTemp.row = i;
                    diffTemp.leftcolB = 0;
                    diffTemp.rightColB = compareTwoArray[i].text.Length;

                    differentList.Add(diffTemp);
                    i++;
                }
                #endregion
            }
            else//这个模式下面只做每行的比对
            {
                //这个while可以单拉出去 然后匹配使用递归思想来实现
                int i = 0;
                while (!srOne.EndOfStream && !srTwo.EndOfStream)
                {
                    Node oneTemp = new Node();
                    Node twoTemp = new Node();
                    oneTemp.index = i;
                    twoTemp.index = i;
                    string strOne = srOne.ReadLine();
                    string strTwo = srTwo.ReadLine();

                    //具体的匹配逻辑
                    if (strOne != strTwo)
                    {
                        DifferentFlag diffTemp = compareStrByChar(strOne, strTwo);
                        diffTemp.row = i;
                        differentList.Add(diffTemp);
                    }
                    oneTemp.text = strOne;
                    twoTemp.text = strTwo;
                    //end

                    compareOneList.Add(oneTemp);
                    compareTwoList.Add(twoTemp);
                    i++;

                }
                //这里还要补充可能其中一个没有遍历完成的
                while (!srOne.EndOfStream)
                {
                    Node oneTemp = new Node();
                    Node twoTemp = new Node();
                    oneTemp.index = i;
                    twoTemp.index = i;
                    oneTemp.text = srOne.ReadLine();
                    twoTemp.text = "";

                    DifferentFlag diffTemp = new DifferentFlag();
                    diffTemp.row = i;
                    diffTemp.leftcolA = 0;
                    diffTemp.rightColA = oneTemp.text.Length;

                    compareOneList.Add(oneTemp);
                    compareTwoList.Add(twoTemp);
                    differentList.Add(diffTemp);
                    i++;
                }
                while (!srTwo.EndOfStream)
                {
                    Node oneTemp = new Node();
                    Node twoTemp = new Node();
                    oneTemp.index = i;
                    twoTemp.index = i;
                    oneTemp.text = "";
                    twoTemp.text = srTwo.ReadLine();

                    DifferentFlag diffTemp = new DifferentFlag();
                    diffTemp.row = i;
                    diffTemp.leftcolB = 0;
                    diffTemp.rightColB = twoTemp.text.Length;
                    compareOneList.Add(oneTemp);
                    compareTwoList.Add(twoTemp);
                    differentList.Add(diffTemp);
                    i++;
                }
                //end
            }
            


            srOne.Close();
            srTwo.Close();
            fileOne.Close();
            fileTwo.Close(); 
        }

        //两个字符串比较获取不同的左边和右边
        public static DifferentFlag compareStrByChar(string p_StrA, string p_StrB)
        {
            DifferentFlag differentFlag = new DifferentFlag();
            char[] arryA = p_StrA.ToCharArray();
            char[] arryB = p_StrB.ToCharArray();

            int lengthA = arryA.Length;
            int lengthB = arryB.Length;
            int subAB = 0;
            bool flag = false;//是否一致标记
            int positionLeft = 1;
            int positionRight = 0;
            if (lengthA >= lengthB)
            {
                subAB = lengthA - lengthB;
                for (int i = 0; i < lengthB; i++)
                {
                    if (arryA[i] != arryB[i])
                    {
                        flag = true;
                        positionLeft = i + 1;
                        break;
                    }
                }
                for (int i = lengthB -1; i >= 0; i--)
                {
                    if (arryA[i + subAB] == arryB[i] && i >= (positionLeft-1))
                    {
                        flag = true;
                        positionRight += 1;
                        
                    }
                    else
                    {
                        break;
                    }
                }
            }
            else
            {
                subAB = lengthB - lengthA;
                for (int i = 0; i < lengthA; i++)
                {
                    if (arryA[i] != arryB[i])
                    {
                        flag = true;
                        positionLeft = i + 1;
                        break;
                    }
                }
                for (int i = lengthA - 1; i >= 0; i--)
                {
                    if (arryA[i] == arryB[i + subAB] && i >= (positionLeft - 1))
                    {
                        flag = true;
                        positionRight += 1;

                    }
                    else
                    {
                        break;
                    }
                    
                }
            }
            //如果找到不一样,则设置位置
            if (flag)
            {
                if (positionLeft > (lengthA - positionRight))
                {
                    if (lengthA >= lengthB)
                    {
                        differentFlag.leftcolA = positionLeft;
                        differentFlag.rightColA = lengthA - positionRight;
                        differentFlag.leftcolB = 0;
                        differentFlag.rightColB = 0;
                    }
                    else
                    {
                        differentFlag.leftcolA = 0;
                        differentFlag.rightColA = 0;
                        differentFlag.leftcolB = positionLeft;
                        differentFlag.rightColB = lengthB - positionRight;
                    }
                }
                else 
                { 
                    differentFlag.leftcolA = positionLeft;
                    differentFlag.leftcolB = positionLeft;
                    differentFlag.rightColA = lengthA - positionRight;
                    differentFlag.rightColB = lengthB - positionRight;
                }
            }
            else//当一边为空的时候进入
            {
                if (lengthB == 0)
                {
                    differentFlag.leftcolA = 0;
                    differentFlag.rightColA = lengthA;
                    differentFlag.leftcolB = 0;
                    differentFlag.rightColB = 0;
                }
                else if(lengthA == 0)
                {
                    differentFlag.leftcolA = 0;
                    differentFlag.rightColA = 0;
                    differentFlag.leftcolB = 0;
                    differentFlag.rightColB = lengthB;
                }
            }

            return differentFlag;
        }

        //文件每行的节点对象
        public class Node
        {
            public int index{get;set;}//所在行数
            public string text { get; set; }//行内容
        }

        //差异点的标记
        public class DifferentFlag{
            public int row{get;set;}//行数
            public int rightColA{get;set;}//右列数
            public int leftcolA{get;set;}//左列数

            public int rightColB { get; set; }//右列数
            public int leftcolB { get; set; }//左列数
        }
       
        //渲染数据到富文本框中
        public void renderData(char[] p_CharA, char[] p_CharB,string p_StrAl,string p_StrAR,string p_StrBL,string p_StrBR)
        {
            Color color;
            //打印左边框的数据
            prinInRichTxt(p_StrAl, p_StrAl.Length, Color.Black);
            for (int i = 0; i < p_CharA.Length; i++)
            {
                color = Color.Black;
                if (difOne.Contains(i))
                {
                    color = Color.Red;
                }
                prinInRichTxt(p_CharA[i].ToString(), 1, color);
                //richTextBox1.AppendText(p_CharA[i].ToString());
                //richTextBox1.SelectionStart = i;
                //richTextBox1.SelectionLength = 1;
                //richTextBox1.SelectionColor = color;
            }
            prinInRichTxt(p_StrAR, p_StrAR.Length, Color.Black);
            richTextBox1.AppendText(System.Environment.NewLine);
            prinInRichTxt(p_StrBL, p_StrBL.Length, Color.Black);
            for (int i = 0; i < p_CharB.Length; i++)
            {
                color = Color.Black;
                if (difTwo.Contains(i))
                {
                    color = Color.Red;
                }
                prinInRichTxt(p_CharB[i].ToString(), 1, color);
                //richTextBox1.AppendText(p_CharB[i].ToString());
                //richTextBox1.SelectionStart = i + p_CharA.Length + 1;
                //richTextBox1.SelectionLength = 1;
                //richTextBox1.SelectionColor = color;
            }
            prinInRichTxt(p_StrBR, p_StrBR.Length, Color.Black);
        }

        //将数据渲染到富文本框中
        public void prinInRichTxt(string p_Content, int p_Length, Color p_Color)
        {
            int lenfthTemp = richTextBox1.TextLength;
            richTextBox1.AppendText(p_Content);
            richTextBox1.SelectionStart = lenfthTemp;
            richTextBox1.SelectionLength = p_Length;
            richTextBox1.SelectionColor = p_Color;
        }

        //两个字符串进行比较 可比较出多个差异点
        public void compareStrByCharMult(string p_StrA, string p_StrB)
        {
            char[] arryA = p_StrA.ToCharArray();
            char[] arryB = p_StrB.ToCharArray();
            int lengthA = arryA.Length;
            int lengthB = arryB.Length;

            int i = 0;
            int j = 0;
            //具体的匹配逻辑类
            Compare compare = new Compare();
            while (i < lengthA && j < lengthB)
            {
                if (arryA[i] != arryB[j])
                {
                    if (compare.isOverSize(i, lengthA, 1))//下一步需要进行下一个字符判断，所以这里需要判断当前是否A字符串的标记是否是最后一位
                    {
                        //是 则记录当前差异字符位置，并结束循环
                        difOne.Add(i);
                        break;
                    }
                    int nextCharIndex = compare.charIndexInStr(arryA[i + 1], p_StrB, j);
                    if (nextCharIndex == 0)//查找A中的下一个字符在B中的位置，返回0则代表不存在
                    {
                        //如果A中的下一个字符不存在，则查找A当前字符是否在B中存在
                        nextCharIndex = compare.charIndexInStr(arryA[i], p_StrB, j + 1);
                        if (nextCharIndex == 0)//如果当前字符也不存在，则直接将A当前位置往后移两位
                        {
                            //这里记录这两个差异字符位置
                            difOne.Add(i);
                            difOne.Add(i + 1);
                            if (compare.isOverSize(i, lengthA, 2))//判断是否超过最大数,防止溢出
                            {
                                //进入这里表示A串到最后两位，且都没有被匹配上 即可跳出循环                               
                                break;
                            }
                            else
                            {
                                //进到这步则表示A字符串还没有比较完
                                i = i + 2;
                                continue;//A数据的标记往后移两位，B的字符串标记不动，继续循环
                            }
                        }
                        else
                        {
                            //进到这里则说明 A当前字符在B中存在，nextCharIndex记录在B中的位置
                            //记录B中的差异点位置，然后移动A,B字符的标志位来进行下一次判断
                            for (int g = j; g < nextCharIndex; g++)
                            {
                                difTwo.Add(g);
                            }
                            j = nextCharIndex + 1;
                            i++;
                        }
                    }
                    else
                    {
                        //进入到这里 说明A当前字符的下一个字符在B串中存在，nextCharIndex变量保存着在B中的位置
                        //如果第二个字符在B中存在,则判断A字符串当前指的字符 是否在j-nextCharIndex中存在
                        int currentACharIndexInB = compare.charIndexInStr(arryA[i], p_StrB, j, nextCharIndex);
                        //currentACharIndexInB记录A当前字符在B中的位置
                        if (currentACharIndexInB == 0)
                        {
                            //进入到这步则说明当前A字符串中的字符不在B中存在
                            //记录差异字符的位置
                            difOne.Add(i);
                            for (int g = j; g < nextCharIndex; g++)
                            {
                                difTwo.Add(g);
                            }
                        }
                        else
                        {
                            //进入这一步则说明A字符串中当前的字符在字符串B中存在
                            //类似的例子是：A：***[b][c]***,B:***adf[b]ji[c]***
                            //i的位置在b，j的位置在a
                            //则记录B中的差异点
                            for (int g = j; g < nextCharIndex; g++)
                            {
                                if (g == currentACharIndexInB) continue;
                                difTwo.Add(g);
                            }

                        }
                        //移动字符标志 进入下一个循环
                        i = i + 2;//移动A,B中的字符标志
                        j = nextCharIndex + 1;
                    }
                }
                else
                {
                    //相等就直接下一位字符比较
                    i++;
                    j++;
                }

            }
            while (i < lengthA)
            {
                difOne.Add(i);
                i++;
            }
            while (j < lengthB)
            {
                difTwo.Add(j);
                j++;
            }
        }

        //找出两个文件的差异行
        public static int[] DiffCodes(string aText, Hashtable h,string[] p_Lines)
        {
            int[] Codes;
            int lastUsedCode = h.Count;
            object aCode;
            string s;

            Codes = new int[p_Lines.Length];

            for (int i = 0; i < p_Lines.Length; ++i)
            {
                s = p_Lines[i];

                aCode = h[s];
                if (aCode == null)
                {
                    lastUsedCode++;
                    h[s] = lastUsedCode;
                    Codes[i] = lastUsedCode;
                }
                else
                {
                    Codes[i] = (int)aCode;
                } 
            } 
           
            return (Codes);
        }

        //预处理需要进行比对的数据
        public static void dealText(int[] p_ArrayA,int[] p_ArrayB,string[] p_LineOne,string[] p_LineTwo)
        {
            int lengthA = p_ArrayA.Length;
            int lengthB = p_ArrayB.Length;
            int i = 0;
            int j = 0;
            int index = 0;
            
            //具体的匹配逻辑类
            Compare compare = new Compare();
            compare.compareByInt(p_ArrayA, p_ArrayB);
            sameArray = compare.mergeData(p_ArrayA, p_ArrayB);
            #region 向a中填充内容
            i = 0;
            j = 0;
            index = 0;
            while(i < sameArray.Length && j < lengthA){
                Node node = new Node();
                node.index = index;
                if (sameArray[i] == p_ArrayA[j])
                {
                    node.text = p_LineOne[j];
                    j++;
                }
                else
                {
                    node.text = "";
                }
                compareOneList.Add(node);
                i++;
                index++;
            }
            while (i < sameArray.Length) {
                Node node = new Node();
                node.index = index;
                node.text = "";
                compareOneList.Add(node);
                i++;
                index++;
            }
            #endregion
            #region 向b中填充内容
            i = 0;
            j = 0;
            index = 0;
            while (i < sameArray.Length && j < lengthB)
            {
                Node node = new Node();
                node.index = index;
                if (sameArray[i] == p_ArrayB[j])
                {
                    node.text = p_LineTwo[j];
                    j++;
                }
                else
                {
                    node.text = "";
                }
                compareTwoList.Add(node);
                i++;
                index++;
            }
            while (i < sameArray.Length)
            {
                Node node = new Node();
                node.index = index;
                node.text = "";
                compareTwoList.Add(node);
                i++;
                index++;
            }
            #endregion
        }

        //初始化内存对象
        private void Init()
        {
            compareOneList.Clear();
            compareTwoList.Clear();
            differentList.Clear();
            difRowFlag = 0;
            listBox1.Items.Clear();
            listBox2.Items.Clear();
            richTextBox1.ResetText();
            linesOne = null;
            linesTwo = null;
            sameArray = null;
            if (radioButton1.Checked)
            {
                comapreMode = true;
            }
            else
            {
                comapreMode = false;
            }
        }


        #region 这里的代码 主要的功能是界面的显示 所以可以不用把重心放在这边
        #region 用于两个listbox框的滚动条的同步
        //创建一个listbox同步的类
        public class SyncListBoxes
        {
            private ListBox _LB1 = null;
            private ListBox _LB2 = null;

            private ListBoxScroll _ListBoxScroll1 = null;
            private ListBoxScroll _ListBoxScroll2 = null;

            public SyncListBoxes(ListBox LB1, ListBox LB2)
            {
                if (LB1 != null && LB1.IsHandleCreated && LB2 != null && LB2.IsHandleCreated &&
                    LB1.Items.Count == LB2.Items.Count && LB1.Height == LB2.Height)
                {
                    this._LB1 = LB1;
                    this._ListBoxScroll1 = new ListBoxScroll(LB1);
                    this._ListBoxScroll1.Scroll += _ListBoxScroll1_VerticalScroll;


                    this._LB2 = LB2;
                    this._ListBoxScroll2 = new ListBoxScroll(LB2);
                    this._ListBoxScroll2.Scroll += _ListBoxScroll2_VerticalScroll;


                    this._LB1.SelectedIndexChanged += _LB1_SelectedIndexChanged;
                    this._LB2.SelectedIndexChanged += _LB2_SelectedIndexChanged;

                }
            }


            private void _LB1_SelectedIndexChanged(object sender, EventArgs e)
            {
                if (this._LB2.TopIndex != this._LB1.TopIndex)
                {
                    this._LB2.TopIndex = this._LB1.TopIndex;
                }
                if (this._LB2.SelectedIndex != this._LB1.SelectedIndex)
                {
                    this._LB2.SelectedIndex = this._LB1.SelectedIndex;
                }
                difRowFlag = this._LB1.SelectedIndex;
            }


            private void _LB2_SelectedIndexChanged(object sender, EventArgs e)
            {
                if (this._LB1.TopIndex != this._LB2.TopIndex)
                {
                    this._LB1.TopIndex = this._LB2.TopIndex;
                }
                if (this._LB1.SelectedIndex != this._LB2.SelectedIndex)
                {
                    this._LB1.SelectedIndex = this._LB2.SelectedIndex;
                }
                difRowFlag = this._LB2.SelectedIndex;
            }


            private void _ListBoxScroll1_VerticalScroll(ListBox LB)
            {
                if (this._LB2.TopIndex != this._LB1.TopIndex)
                {
                    this._LB2.TopIndex = this._LB1.TopIndex;
                }
            }


            private void _ListBoxScroll2_VerticalScroll(ListBox LB)
            {
                if (this._LB1.TopIndex != this._LB2.TopIndex)
                {
                    this._LB1.TopIndex = this._LB2.TopIndex;
                }
            }


            private class ListBoxScroll : NativeWindow
            {


                private ListBox _LB = null;
                private const int WM_VSCROLL = 0x115;
                private const int WM_MOUSEWHEEL = 0x20a;


                public event dlgListBoxScroll Scroll;
                public delegate void dlgListBoxScroll(ListBox LB);


                public ListBoxScroll(ListBox LB)
                {
                    this._LB = LB;
                    this.AssignHandle(LB.Handle);
                }


                protected override void WndProc(ref Message m)
                {
                    base.WndProc(ref m);
                    switch (m.Msg)
                    {
                        case WM_VSCROLL:
                        case WM_MOUSEWHEEL:
                            if (this.Scroll != null)
                            {
                                this.Scroll(_LB);
                            }
                            break;
                    }
                }


            }
        }
        private void listBox2_SelectedIndexChanged(object sender, EventArgs e)
        {

            //printDifInRichText(listBox2.SelectedIndex);
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            //printDifInRichText();
            richTextBox1.ResetText();
            DifferentFlag different = differentList.FirstOrDefault(a => a.row == listBox1.SelectedIndex);
            if (different != null)
            {
                difOne.Clear();
                difTwo.Clear();
                string strAL = "";
                string strAR = "";
                string strBL = "";
                string strBR = "";

                string richStrA = compareOneList.FirstOrDefault(a => a.index == listBox1.SelectedIndex).text;//理论上来说这个是一定有值的 所以不做错误处理 后期可以加上
                string richStrB = compareTwoList.FirstOrDefault(a => a.index == listBox1.SelectedIndex).text;

                if (comapreMode)
                {
                    if (richStrA != "" && richStrB == "" && differentList.FirstOrDefault(a => a.row == (listBox1.SelectedIndex + 1)) != null)
                    {
                        richStrB = compareTwoList.FirstOrDefault(a => a.index == (listBox1.SelectedIndex + 1)).text;
                    }
                    if (richStrA == "" && richStrB != "" && differentList.FirstOrDefault(a => a.row == (listBox1.SelectedIndex - 1)) != null)
                    {
                        richStrA = compareOneList.FirstOrDefault(a => a.index == (listBox1.SelectedIndex - 1)).text;
                    }
                    different = compareStrByChar(richStrA, richStrB);
                }

                int leftColAtemp = different.leftcolA == 0 ? 0 : different.leftcolA - 1;
                int leftColBtemp = different.leftcolB == 0 ? 0 : different.leftcolB - 1;



                strAL = richStrA.Substring(0, leftColAtemp);
                strAR = richStrA.Substring(different.rightColA, richStrA.Length - different.rightColA);

                strBL = richStrB.Substring(0, leftColBtemp);
                strBR = richStrB.Substring(different.rightColB, richStrB.Length - different.rightColB);



                richStrA = richStrA.Substring(leftColAtemp, different.rightColA - leftColAtemp);
                richStrB = richStrB.Substring(leftColBtemp, different.rightColB - leftColBtemp);
                compareStrByCharMult(richStrA, richStrB);//比较数据
                renderData(richStrA.ToArray(), richStrB.ToArray(), strAL, strAR, strBL, strBR);//渲染数据
            }

        }
        #endregion

        #region 用于文本的渲染
        private void listBox1_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index >= 0)
            {
                e.DrawBackground();
                Brush mybsh = Brushes.Black;
                string context = listBox1.Items[e.Index].ToString();
                string blackStr = "";
                // 判断是什么类型的标签
                DifferentFlag diffTemp = differentList.FirstOrDefault(a => a.row == e.Index);
                string redStrTemp = context;
                if (diffTemp != null && context != "")
                {
                    mybsh = Brushes.Red;
                    int leftTemp = diffTemp.leftcolA == 0 ? 0 : diffTemp.leftcolA - 1;
                    redStrTemp = context.Substring(leftTemp, diffTemp.rightColA - leftTemp);
                    blackStr = context.Substring(0, leftTemp) + string.Empty.PadRight(Encoding.Default.GetByteCount(redStrTemp), ' ') + context.Substring(diffTemp.rightColA, context.Length - diffTemp.rightColA);
                    redStrTemp = string.Empty.PadRight(leftTemp, ' ') + redStrTemp + string.Empty.PadRight(context.Length - diffTemp.rightColA, ' ');
                    //rightStr = string.Empty.PadRight(diffTemp.rightCol, ' ') + context.Substring(diffTemp.rightCol, context.Length - diffTemp.rightCol);


                    e.Graphics.DrawString(blackStr, e.Font, Brushes.Black, e.Bounds, StringFormat.GenericDefault);
                }
                // 焦点框
                //e.DrawFocusRectangle();
                //文本 

                e.Graphics.DrawString(redStrTemp, e.Font, mybsh, e.Bounds, StringFormat.GenericDefault);
            }
        }
        private void listBox2_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index >= 0)
            {
                e.DrawBackground();
                Brush mybsh = Brushes.Black;
                string context = listBox2.Items[e.Index].ToString();
                string blackStr = "";
                // 判断是什么类型的标签
                DifferentFlag diffTemp = differentList.FirstOrDefault(a => a.row == e.Index);
                string redStrTemp = context;
                if (diffTemp != null && context != "")
                {
                    mybsh = Brushes.Red;
                    int leftTemp = diffTemp.leftcolB == 0 ? 0 : diffTemp.leftcolB - 1;
                    redStrTemp = context.Substring(leftTemp, diffTemp.rightColB - leftTemp);
                    blackStr = context.Substring(0, leftTemp) + string.Empty.PadRight(Encoding.Default.GetByteCount(redStrTemp), ' ') + context.Substring(diffTemp.rightColB, context.Length - diffTemp.rightColB);
                    context = string.Empty.PadRight(leftTemp, ' ') + redStrTemp + string.Empty.PadRight(context.Length - diffTemp.rightColB, ' ');

                    e.Graphics.DrawString(blackStr, e.Font, Brushes.Black, e.Bounds, StringFormat.GenericDefault);
                }
                // 焦点框
                //e.DrawFocusRectangle();
                //文本 
                e.Graphics.DrawString(context, e.Font, mybsh, e.Bounds, StringFormat.GenericDefault);
            }
        }
        #endregion

        #region 无用代码
        public void printDifInRichText(int p_CurrentIndex)
        {
            richTextBox1.ResetText();
            DifferentFlag different = differentList.FirstOrDefault(a => a.row == p_CurrentIndex);
            if (different != null)
            {
                string richStrA = compareOneList.FirstOrDefault(a => a.index == p_CurrentIndex).text;//理论上来说这个是一定有值的 所以不做错误处理 后期可以加上
                string richStrB = compareTwoList.FirstOrDefault(a => a.index == p_CurrentIndex).text;

                different.leftcolA = different.leftcolA == 0 ? 0 : different.leftcolA - 1;
                richTextBox1.AppendText(richStrA);
                richTextBox1.SelectionStart = different.leftcolA;
                richTextBox1.SelectionLength = different.rightColA - different.leftcolA;
                richTextBox1.SelectionColor = Color.Red;

                richTextBox1.AppendText(System.Environment.NewLine);

                different.leftcolB = different.leftcolB == 0 ? 0 : different.leftcolB - 1;
                richTextBox1.AppendText(richStrB);
                richTextBox1.SelectionStart = richStrA.Length + 1 + different.leftcolB;
                richTextBox1.SelectionLength = different.rightColB - different.leftcolB;
                richTextBox1.SelectionColor = Color.Red;
            }
        }
        #endregion
        #endregion

    }

}
