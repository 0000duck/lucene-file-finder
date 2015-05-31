using System;
using LuceneFileFinder.Type;

namespace LuceneFileFinder.Util
{
    /// <summary>
    /// �ļ���Ŀ¼�ı�����
    /// ������ΪWin32API���� FindFirstFile , FindNextFile�ķ�װ
    /// </summary>
    public class FileDirectoryFinder
    {
        private FindFileData currentFileData = new FindFileData();

        private bool bolIsFile = true;
        /// <summary>
        /// ��ǰ�����Ƿ�Ϊ�ļ�,��Ϊtrue��ǰ����Ϊ�ļ�,����ΪĿ¼
        /// </summary>
        public bool IsFile
        {
            get { return this.bolIsFile; }
        }

        #region ���ƶ������Ե�һЩ���� ****************************************

        private string strSearchPath = null;
        /// <summary>
        /// �����ĸ�Ŀ¼,����Ϊ����·��,������ͨ���,��Ŀ¼�������
        /// </summary>
        public string SearchPath
        {
            get { return strSearchPath; }
            set { strSearchPath = value; }
        }
        #endregion

        /// <summary>
        /// ��ǰΪ�ļ���ʱ�������ļ���ȫ��
        /// </summary>
        public string FullName
        {
            get { return this.strSearchPath +  this.myData.cFileName + "\\"; }
        }
        /// <summary>
        /// �ҵ����ļ�������
        /// </summary>
        public FindFileData CurrentFileData
        {
            get { return this.currentFileData; }
        }

        /// <summary>
        /// �رն���,ֹͣ����
        /// </summary>
        public void Close()
        {
            this.CloseHandler();
        }

        /// <summary>
        /// �ҵ���һ���ļ���Ŀ¼
        /// </summary>
        /// <returns>�����Ƿ�ɹ�</returns>
        public bool MoveNext()
        {
            bool success = false;
            while (true)
            {
                if (this.bolStartSearchFlag)
                    success = this.SearchNext();
                else
                    success = this.StartSearch();
                if (success)
                {
                    if (this.UpdateCurrentObject())
                    {
                        //���µ�ǰ�ļ�����
                        this.currentFileData = new FindFileData(myData, this.strSearchPath, this.bolIsFile);
                        return true;
                    }
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// �������ö���
        /// </summary>
        public void Reset()
        {
            //if (this.strSearchPath == null)
            //    return;
                //throw new System.ArgumentNullException("SearchPath can not null");

            this.CloseHandler();
            this.bolStartSearchFlag = false;
        }

        #region ����WIN32API����

        [System.Runtime.InteropServices.DllImport
            ("kernel32.dll",
            CharSet = System.Runtime.InteropServices.CharSet.Auto,
            SetLastError = true)]
        private static extern IntPtr FindFirstFile(string pFileName, ref WIN32_FIND_DATA pFindFileData);

        [System.Runtime.InteropServices.DllImport
            ("kernel32.dll",
           CharSet = System.Runtime.InteropServices.CharSet.Auto,
            SetLastError = true)]
        private static extern bool FindNextFile(IntPtr hndFindFile, ref WIN32_FIND_DATA lpFindFileData);

        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FindClose(IntPtr hndFindFile);

        #endregion

        #region �ڲ�����Ⱥ

        private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
        /// <summary>
        /// ���Ҵ���ĵײ���
        /// </summary>
        private System.IntPtr intSearchHandler = INVALID_HANDLE_VALUE;

        private WIN32_FIND_DATA myData = new WIN32_FIND_DATA();
        /// <summary>
        /// ��ʼ������־
        /// </summary>
        private bool bolStartSearchFlag = false;
        /// <summary>
        /// �ر��ڲ����
        /// </summary>
        private void CloseHandler()
        {
            if (this.intSearchHandler != INVALID_HANDLE_VALUE)
            {
                FindClose(this.intSearchHandler);
                this.intSearchHandler = INVALID_HANDLE_VALUE;
            }
        }
        /// <summary>
        /// ��ʼ����
        /// </summary>
        /// <returns>�����Ƿ�ɹ�</returns>
        private bool StartSearch()
        {
            bolStartSearchFlag = true;

            this.CloseHandler();
            intSearchHandler = FindFirstFile(this.strSearchPath + "*", ref myData);
            if (intSearchHandler == INVALID_HANDLE_VALUE)
            {
                return false;
            }
            return true;
        }
        /// <summary>
        /// ������һ��
        /// </summary>
        /// <returns>�����Ƿ�ɹ�</returns>
        private bool SearchNext()
        {
            if (bolStartSearchFlag == false)
                return false;
            if (intSearchHandler == INVALID_HANDLE_VALUE)
                return false;
            try
            {
                if (FindNextFile(intSearchHandler, ref myData) == false)
                {
                    this.CloseHandler();
                    return false;
                }
            }
            catch
            {
                return false;
            }
            return true;
        }//private bool SearchNext()

        /// <summary>
        /// ���µ�ǰ����
        /// </summary>
        /// <returns>�����Ƿ�ɹ�</returns>
        private bool UpdateCurrentObject()
        {
            if (intSearchHandler == INVALID_HANDLE_VALUE)
                return false;
            else if (myData.cFileName == null)
                return false;
            else if ((myData.dwFileAttributes & 0x10) == 0)
            {
                // ��ǰ����Ϊ�ļ�
                this.bolIsFile = true;
                return true;
            }
            else
            {
                // ��ǰ����ΪĿ¼
                this.bolIsFile = false;
                if (myData.cFileName == "." || myData.cFileName == "..")
                    return false;
                else
                    return true;
            }
        }//private bool UpdateCurrentObject()

        #endregion
    }
}