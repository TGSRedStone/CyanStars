using CyanStars.Framework.Timeline;
using CyanStars.Gameplay.Input;

namespace CyanStars.Gameplay.Note
{
    /// <summary>
    /// 音符轨道
    /// </summary>
    public class NoteTrack : BaseTrack
    {
        /// <summary>
        /// 创建音符轨道片段
        /// </summary>
        public static readonly IClipCreator<NoteTrack, MusicTimelineData> ClipCreator = new NoteClipCreator();

        private sealed class NoteClipCreator : IClipCreator<NoteTrack, MusicTimelineData>
        {
            public BaseClip<NoteTrack> CreateClip(NoteTrack track, int clipIndex, MusicTimelineData data)
            {
                NoteClip clip = new NoteClip(0, data.Time / 1000f, track, data.BaseSpeed, data.SpeedRate);

                for (int i = 0; i < data.LayerDatas.Count; i++)
                {
                    LayerData layerData = data.LayerDatas[i];
                    NoteLayer layer = new NoteLayer();

                    for (int j = 0; j < layerData.ClipDatas.Count; j++)
                    {
                        ClipData clipData = layerData.ClipDatas[j];
                        layer.AddTimeSpeedRate(clipData.StartTime / 1000f, clipData.SpeedRate);

                        for (int k = 0; k < clipData.NoteDatas.Count; k++)
                        {
                            NoteData noteData = clipData.NoteDatas[k];

                            BaseNote note = CreateNote(noteData, layer);
                            layer.AddNote(note);
                        }
                    }

                    clip.AddLayer(layer);
                }

                return clip;
            }
        }

        /// <summary>
        /// 根据音符数据创建音符
        /// </summary>
        private static BaseNote CreateNote(NoteData noteData, NoteLayer layer)
        {
            BaseNote note = null;
            switch (noteData.Type)
            {
                case NoteType.Tap:
                    note = new TapNote();
                    break;
                case NoteType.Hold:
                    note = new HoldNote();
                    break;
                case NoteType.Drag:
                    note = new DragNote();
                    break;
                case NoteType.Click:
                    note = new ClickNote();
                    break;
                case NoteType.Break:
                    note = new BreakNote();
                    break;
            }

            note.Init(noteData, layer);
            return note;
        }

        public void OnInput(InputType inputType, InputMapData.Item item)
        {
            if (clips.Count == 0)
            {
                return;
            }

            NoteClip clip = (NoteClip)clips[0];
            clip.OnInput(inputType, item);
        }
    }
}