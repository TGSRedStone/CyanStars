﻿using UnityEngine;
using UnityEngine.SceneManagement;

namespace CatAsset.Runtime
{
    /// <summary>
    /// 场景加载任务完成回调的原型
    /// </summary>
    public delegate void LoadSceneCallback(bool success, Scene scene);
    
    /// <summary>
    /// 场景加载任务
    /// </summary>
    public class LoadSceneTask : LoadBundledAssetTask<object>
    {
        private LoadSceneCallback onFinished;
        private Scene loadedScene;

        public override void Run()
        {
            if (AssetRuntimeInfo.RefCount > 0)
            {
                //引用计数 > 0
                //跳过依赖的加载 直接加载场景
                LoadState = LoadBundleAssetState.DependenciesLoaded;
            }
            else
            {
                base.Run();
            }
            
        }

        /// <inheritdoc />
        protected override void LoadAsync()
        {
            Operation = SceneManager.LoadSceneAsync(Name, LoadSceneMode.Additive);
        }

        /// <inheritdoc />
        protected override void LoadDone()
        {
            if (Operation != null)
            {
                //Operation不为null就是场景加载成功了
                loadedScene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);
                SceneManager.SetActiveScene(loadedScene);
                CatAssetDatabase.SetSceneInstance(loadedScene,AssetRuntimeInfo);
            }
           
        }

        /// <inheritdoc />
        protected override bool IsLoadFailed()
        {
            return Operation == null;
        }
        
        /// <inheritdoc />
        protected override void CallFinished(bool success)
        {
            if (!success)
            {
                //加载失败 通知所有未取消的加载任务
                if (!NeedCancel)
                {
                    onFinished?.Invoke(default,default);
                }
                
                foreach (LoadSceneTask task in MergedTasks)
                {
                    if (!task.NeedCancel)
                    {
                        task.onFinished?.Invoke(default,default);
                    }
                }
            }
            else
            {
                AssetRuntimeInfo.AddRefCount();
                if (NeedCancel)
                {
                    //被取消了 卸载场景
                    CatAssetManager.UnloadScene(loadedScene);
                }
                else
                {
                    onFinished?.Invoke(true, loadedScene);
                }
                
                //加载成功后 无论主任务是否被取消 都要对已合并任务调用LoadSceneAsync重新走加载场景流程
                //因为每次加载场景都是在实例化一个新场景 无法复用
                foreach (LoadSceneTask task in MergedTasks)
                {
                    if (!task.NeedCancel)
                    {
                        CatAssetManager.LoadSceneAsync(task.Name,task.onFinished);
                    }
                }
            }
        }

        /// <summary>
        /// 创建场景加载任务的对象
        /// </summary>
        public static LoadSceneTask Create(TaskRunner owner, string name,LoadSceneCallback callback)
        {
            LoadSceneTask task = ReferencePool.Get<LoadSceneTask>();
            task.CreateBase(owner,name);

            task.AssetRuntimeInfo = CatAssetDatabase.GetAssetRuntimeInfo(name);
            task.BundleRuntimeInfo =
                CatAssetDatabase.GetBundleRuntimeInfo(task.AssetRuntimeInfo.BundleManifest.RelativePath);
            task.onFinished = callback;

            return task;
        }

        public override void Clear()
        {
            base.Clear();
            
            onFinished = default;
            loadedScene = default;
        }
    }
}