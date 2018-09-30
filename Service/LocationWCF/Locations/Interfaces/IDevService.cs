﻿using System.Collections.Generic;
using System.ServiceModel;
using Location.TModel.Location.AreaAndDev;

namespace LocationServices.Locations.Interfaces
{
    [ServiceContract]
    public interface IDevService
    {
        #region 设备位置(DevPos)和信息(DevInfo)的增删改查
        /// <summary>
        /// 获取模型类型数量
        /// </summary>
        /// <returns></returns>
        [OperationContract]
        ObjectAddList GetObjectAddList();

        /// <summary>
        /// 获取所有设备的位置信息
        /// </summary>
        /// <returns></returns>
        [OperationContract]
        IList<DevPos> GetDevPositions();
        /// <summary>
        /// 添加一条设备位置信息
        /// </summary>
        /// <param name="pos"></param>
        [OperationContract]
        bool AddDevPosInfo(DevPos pos);
        /// <summary>
        /// 添加设备位置信息（列表形式）
        /// </summary>
        /// <param name="posList"></param>
        [OperationContract]
        bool AddDevPosByList(List<DevPos> posList);
        /// <summary>
        /// 修改设备信息
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        [OperationContract]
        bool ModifyPosInfo(DevPos pos);
        /// <summary>
        /// 修改设备位置信息，列表方式
        /// </summary>
        /// <param name="posList"></param>
        /// <returns></returns>
        [OperationContract]
        bool ModifyPosByList(List<DevPos> posList);
        /// <summary>
        /// 删除设备位置信息
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        [OperationContract]
        bool DeletePosInfo(DevPos pos);
        /// <summary>
        /// 获取所有的设备基本信息
        /// </summary>
        /// <returns></returns>
        [OperationContract]
        IList<DevInfo> GetAllDevInfos();
        /// <summary>
        /// 获取所有的设备基本信息
        /// </summary>
        /// <returns></returns>
        [OperationContract]
        IList<DevInfo> GetDevInfos(int[] typeList);
        /// <summary>
        /// 获取所有的设备基本信息
        /// </summary>
        /// <returns></returns>
        [OperationContract]
        IList<DevInfo> FindDevInfos(string key);
        /// <summary>
        /// 添加一条设备基本信息
        /// </summary>
        /// <param name="devInfo"></param>
        [OperationContract]
        DevInfo AddDevInfo(DevInfo devInfo);
        /// <summary>
        /// 添加设备基本信息（列表形式）
        /// </summary>
        /// <param name="devInfoList"></param>
        [OperationContract]
        bool AddDevInfoByList(List<DevInfo> devInfoList);
        /// <summary>
        /// 修改设备信息
        /// </summary>
        /// <param name="devInfo"></param>
        /// <returns></returns>
        [OperationContract]
        bool ModifyDevInfo(DevInfo devInfo);
        /// <summary>
        /// 删除设备信息
        /// </summary>
        /// <param name="devInfo"></param>
        /// <returns></returns>
        [OperationContract]
        bool DeleteDevInfo(DevInfo devInfo);
        /// <summary>
        /// 根据ID，获取区域ID下所有设备
        /// </summary>
        /// <param name="pids"></param>
        /// <returns></returns>
        [OperationContract]
        IList<DevInfo> GetDevInfoByParent(int[] pids);

        //门禁设备的增删改查

        /// <summary>
        /// 添加门禁
        /// </summary>
        /// <param name="doorAccessList"></param>
        /// <returns></returns>
        [OperationContract]
        bool AddDoorAccessByList(IList<Dev_DoorAccess> doorAccessList);
        /// <summary>
        /// 添加门禁信息
        /// </summary>
        /// <param name="doorAccess"></param>
        /// <returns></returns>
        [OperationContract]
        bool AddDoorAccess(Dev_DoorAccess doorAccess);
        /// <summary>
        /// 删除门禁
        /// </summary>
        /// <param name="doorAccessList"></param>
        /// <returns></returns>
        [OperationContract]
        bool DeleteDoorAccess(IList<Dev_DoorAccess> doorAccessList);
        /// <summary>
        /// 修改门禁信息
        /// </summary>
        /// <param name="doorAccessList"></param>
        /// <returns></returns>
        [OperationContract]
        bool ModifyDoorAccess(IList<Dev_DoorAccess> doorAccessList);
        /// <summary>
        /// 通过区域ID，获取所有门禁信息
        /// </summary>
        /// <param name="pids"></param>
        /// <returns></returns>
        [OperationContract]
        IList<Dev_DoorAccess> GetDoorAccessInfoByParent(int[] pids);
        #endregion
    }
}