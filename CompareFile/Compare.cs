using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompareFile
{
    public class Compare
    {
        //存放文件差异点的位置
        //private static List<DifferentFlag> differentList = new List<DifferentFlag>();

        //差异行的行号记录 某种意义上来说可以算一个通用变量了把
        private static List<int> difOne = new List<int>();
        private static List<int> difTwo = new List<int>();
        //根据字符比较两个字符串
        public void compareStrByChar(string p_StrA, string p_StrB)
        {
            char[] arryA = p_StrA.ToCharArray();
            char[] arryB = p_StrB.ToCharArray();
            int lengthA = arryA.Length;
            int lengthB = arryB.Length;

            int i = 0;
            int j = 0;
            while (i < lengthA && j < lengthB)
            {
                if (arryA[i] != arryB[j])
                {
                    if (isOverSize(i, lengthA, 1))//下一步需要进行下一个字符判断，所以这里需要判断当前是否A字符串的标记是否是最后一位
                    {
                        //是 则记录当前差异字符位置，并结束循环
                        difOne.Add(i);
                        break;
                    }
                    int nextCharIndex = charIndexInStr(arryA[i + 1], p_StrB, j);
                    if (nextCharIndex == 0)//查找A中的下一个字符在B中的位置，返回0则代表不存在
                    {
                        //如果A中的下一个字符不存在，则查找A当前字符是否在B中存在
                        nextCharIndex = charIndexInStr(arryA[i], p_StrB, j + 1);
                        if (nextCharIndex == 0)//如果当前字符也不存在，则直接将A当前位置往后移两位
                        {
                            //这里记录这两个差异字符位置
                            difOne.Add(i);
                            difOne.Add(i + 1);
                            if (isOverSize(i, lengthA, 2))//判断是否超过最大数,防止溢出
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
                        int currentACharIndexInB = charIndexInStr(arryA[i], p_StrB, j, nextCharIndex);
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
        //功能同上
        public void compareByInt(int[] p_ArrayA, int[] p_ArrayB)
        {
            int lengthA = p_ArrayA.Length;
            int lengthB = p_ArrayB.Length;

            int i = 0;
            int j = 0;
            while (i < lengthA && j < lengthB)
            {
                if (p_ArrayA[i] != p_ArrayB[j])
                {
                    if (isOverSize(i, lengthA, 1))//下一步需要进行下一个字符判断，所以这里需要判断当前是否A字符串的标记是否是最后一位
                    {
                        //是 则记录当前差异字符位置，并结束循环
                        difOne.Add(i);
                        break;
                    }
                    int nextCharIndex = charIndexInStr(p_ArrayA[i + 1], p_ArrayB, j);
                    if (nextCharIndex == 0)//查找A中的下一个字符在B中的位置，返回0则代表不存在
                    {
                        //如果A中的下一个字符不存在，则查找A当前字符是否在B中存在
                        nextCharIndex = charIndexInStr(p_ArrayA[i], p_ArrayB, j + 1);
                        if (nextCharIndex == 0)//如果当前字符也不存在，则直接将A当前位置往后移两位
                        {
                            //这里记录这两个差异字符位置
                            difOne.Add(i);
                            difOne.Add(i + 1);
                            if (isOverSize(i, lengthA, 2))//判断是否超过最大数,防止溢出
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
                        int currentACharIndexInB = charIndexInStr(p_ArrayA[i], p_ArrayB, j, nextCharIndex);
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

        //查找字符出现的第一个位置
        public int charIndexInStr(char p_Char, string p_Str, int p_StartIndex)
        {
            char[] arrayStr = p_Str.ToCharArray();
            int flag = 0;
            for (int k = p_StartIndex; k < arrayStr.Length; k++)
            {
                if (p_Char == arrayStr[k])
                {
                    flag = k;
                    break;
                }
            }

            return flag;
        }
        //上面方法的重载
        public int charIndexInStr(int p_Char, int[] p_Array, int p_StartIndex)
        {
            int flag = 0;
            for (int k = p_StartIndex; k < p_Array.Length; k++)
            {
                if (p_Char == p_Array[k])
                {
                    flag = k;
                    break;
                }
            }

            return flag;
        }

        //在位置范围内查找字符是否在字符串中出现
        public int charIndexInStr(char p_Char, string p_Str, int p_StartIndex, int p_EndIndex)
        {
            char[] arrayStr = p_Str.ToCharArray();
            int flag = 0;
            for (int k = p_StartIndex; k <= p_EndIndex; k++)
            {
                if (p_Char == arrayStr[k])
                {
                    flag = k;
                    break;
                }
            }

            return flag;
        }
        //上面方法的重载
        public int charIndexInStr(int p_Char, int[] p_Array, int p_StartIndex, int p_EndIndex)
        {
            int flag = 0;
            for (int k = p_StartIndex; k <= p_EndIndex; k++)
            {
                if (p_Char == p_Array[k])
                {
                    flag = k;
                    break;
                }
            }

            return flag;
        }

        //是否超过最大数
        public bool isOverSize(int p_Index, int p_Length, int p_AddIndex = 0)
        {
            if ((p_Index + p_AddIndex) >= p_Length)
                return true;
            else return false;
        }

        //返回两个对比数据的并集
        public int[] mergeData(int[] p_ArrayA, int[] p_ArrayB)
        {
            int lengthA = p_ArrayA.Length;
            int lengthB = p_ArrayB.Length;
            int mergeLength = (lengthA + lengthB) - (lengthA - difOne.Count);
            int[] mergeArray = new int[mergeLength];
            int i = 0;
            int j = 0;
            int index = 0;
            while (i < lengthA && j < lengthB)
            {
                if (p_ArrayA[i] != p_ArrayB[j])
                {
                    if (difOne.Contains(i))
                    {
                        mergeArray[index] = p_ArrayA[i];
                        i++;
                        index++;
                    }
                    if (difTwo.Contains(j))
                    {
                        mergeArray[index] = p_ArrayB[j];
                        j++;
                        index++;
                    }
                }
                else
                {
                    mergeArray[index] = p_ArrayA[i];
                    index++;
                    i++;
                    j++;
                }
            }
            while (i < lengthA)
            {
                mergeArray[index] = p_ArrayA[i];
                i++;
                index++;
            }
            while (j < lengthB)
            {
                mergeArray[index] = p_ArrayB[j];
                j++;
                index++;
            }
            return mergeArray;
        }

        /// <summary>
        /// 差异点的标记
        /// </summary>
        public class DifferentFlag
        {
            public int row { get; set; }//第几个数
            public int rightColA { get; set; }//右结束位置
            public int leftcolA { get; set; }//左开始位置

            public int rightColB { get; set; }//右结束位置
            public int leftcolB { get; set; }//左开始位置
        }
    }
}
