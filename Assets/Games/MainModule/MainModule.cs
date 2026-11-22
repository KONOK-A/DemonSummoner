using System;
using System.Collections.Generic;
using Games.MainModule.Scripts.Controller;
using Games.MainModule.Scripts.Utility;
using XFGameFramework;

namespace Games.MainModule
{
    public class MainModule : Module
    { 
        // 当前模块所使用的资源模块名称
        public override string ProjectName => "MainModule";
     
        // 是否为基础模块，基础模块不可关闭，如果模块能够关闭，请设置为false
        public override bool BaseModule => false;

        // 需要预加载的表(仅支持json) key:json文件名称 value:类型
        public override Dictionary<string, Type> PreloadConfigTables => base.PreloadConfigTables;

        // 当模块启动时触发
        public override void OnStart()
        {
            if (DisplayController.Supported) LoadController<DisplayController>();
            LoadController<GameController>().SetGameState(GameState.Start);
        }
    }
}
