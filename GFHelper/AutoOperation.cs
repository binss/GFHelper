﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GFHelper.Models;
using System.Threading;

namespace GFHelper
{
    class AutoOperation
    {

        private InstanceManager im;
        public List<AutoOperationInfo> operationList;
        public bool autoOperation;
        private bool isRefresh;

        public AutoOperation(InstanceManager im)
        {
            this.autoOperation = false;
            this.isRefresh = false;
            this.im = im;
            this.operationList = new List<AutoOperationInfo>();
            im.mainWindow.listViewOperation.ItemsSource = operationList;
            im.mainWindow.listViewOperation.DataContext = operationList;
        }

        public void StartRefresh()
        {
            Task.Run(() =>
            {
                this.isRefresh = true;
                Refresh();
            });
        }

        public void SetTeamInfo()
        {
            im.mainWindow.Dispatcher.Invoke(() =>
            {
                im.mainWindow.comboBoxOperationTeam.Items.Clear();
            });
            
            foreach(var i in Data.userInfo.teamInfo)
            {
                var item = i.Value;
                string itemtext;
                itemtext = String.Format("梯队{0}({1})", i.Key, Data.gunInfo[item[1].GunID].name);
                im.uiHelper.addComboBoxText(im.mainWindow.comboBoxOperationTeam, itemtext);
            }
            
        }

        public void SetOperationInfo()
        {
            for (int i = 1; i <= Data.operationInfo.Count; ++i)
            {
                var item = Data.operationInfo[i];
                string itemtext;
                itemtext = String.Format("{0}({1}-{2})", item.name, (int)((item.id - 1) / 4), (int)((item.id - 1 )% 4) + 1);
                im.uiHelper.addComboBoxText(im.mainWindow.comboBoxOperation, itemtext);
            }
        }

        public void Start(int team, int operation)
        {
            /*
            outdatacode: {"team_id":1,"operation_id":8}
            Serverdata: 1
            */
            AutoOperationInfo ao = new AutoOperationInfo(team, operation);
            //ao.LastTime = Data.operationInfo[operation].duration;
            Start(ao);            
        }

        public void AddTimerStartOperation(AutoOperationInfo ao)
        {
            im.timer.AddTimer(CommonHelper.ConvertDateTimeInt(DateTime.Now), ao.LastTime, this.EndDelegate, (object)ao, ao.getTextBlock());
            im.timer.Start();

            this.operationList.Add(ao);
        }

        public void Start(AutoOperationInfo ao)
        {
            string res = StartOperation(ao);
            if (res == "1")
            {
                AddTimerStartOperation(ao);

            }
            else
            {
                im.uiHelper.setStatusBarText_InThread(String.Format("[{0}]远征 {1} 开始失败！请检查相关参数！错误信息: {2}" ,DateTime.Now.ToString(), ao.OperationName, res));
                im.logger.Log(String.Format("远征开始失败，错误原因:{0}", res));
            }

        }

        public void EndDelegate(object a)
        {
            try {
                AutoOperationInfo ao = (AutoOperationInfo)a;
                if (!autoOperation)
                {
                    return;
                }

                Thread.Sleep(1000);//等待一秒
                var op = FindOperation(ao._operationId);
                if (op != null)
                {
                    EndOperation(ao);
                    operationList.Remove(op);
                }
                else
                    return;

                Thread.Sleep(GetWaitSec());//暂停

                im.mainWindow.Dispatcher.Invoke(() =>
                {
                    ao.SetDefaultLastTime();
                    Start(ao);
                });
 
            }
            catch(Exception e)
            {
                im.logger.Log(e);
            }

        }

        public AutoOperationInfo FindOperation(int operation)
        {
            foreach(var item in operationList.ToArray())
            {
                if(item._operationId == operation)
                {
                    return item;
                }
            }

            return null;
        }

        private string StartOperation(AutoOperationInfo ao)
        {
            string jsondata = String.Format("{{\"team_id\":{0},\"operation_id\":{1},\"mission_id\":{2}}}", ao._teamId, ao._operationId, ao.MissionId);

            string result = im.serverHelper.SendDataToServer(RequestUrls.StartOperation, jsondata);
            return result;
        }

        private bool EndOperation(AutoOperationInfo ao)
        {
            try
            {
                string jsondata = String.Format("{{\"operation_id\":{0}}}", ao._operationId.ToString());

                string result = im.serverHelper.SendDataToServer(RequestUrls.FinishOperation, jsondata);
                im.logger.Log(result);
                return true;
            }
            catch(Exception e)
            {
                im.logger.Log(e);
                return false;
            }

        }

        private void Refresh()
        {
            while (isRefresh)
            {
                im.uiHelper.refreshListOperation();
                Thread.Sleep(500);
            }
            
        }

        private int GetWaitSec()
        {
            int min = 2000;
            int max = im.configManager.getConfigInt("maxwaittime");
            max = (max > min) ? max : 5000;//默认值5000
            return new Random().Next(min, max);
        }
    }
}
