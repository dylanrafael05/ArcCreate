using System;
using System.Collections.Generic;
using System.Linq;
using ArcCreate.ChartFormat;
using ArcCreate.Gameplay.Chart;
using ArcCreate.Gameplay.Data;
using UnityEngine;

namespace ArcCreate.Compose.Project
{
    public class RawEventsBuilder
    {
        public List<(RawTimingGroup groups, IEnumerable<RawEvent> events)> GetEvents(string filterForFile = null, bool requireEditable = true)
        {
            List<(RawTimingGroup groups, IEnumerable<RawEvent> events)> list = new List<(RawTimingGroup groups, IEnumerable<RawEvent> events)>();
            Dictionary<RawTimingGroup, TimingGroup> rawMap = new Dictionary<RawTimingGroup, TimingGroup>();

            foreach (TimingGroup tg in Services.Gameplay.Chart.TimingGroups)
            {
                bool correctFile = filterForFile == null || tg.GroupProperties.FileName == filterForFile;
                bool satisfyEditable = !requireEditable || tg.GroupProperties.Editable;
                bool shouldProcess = correctFile && satisfyEditable;
                if (!shouldProcess)
                {
                    continue;
                }

                RawTimingGroup rawprop = tg.GroupProperties.ToRaw();
                rawMap.Add(rawprop, tg);

                List<ArcEvent> events = new List<ArcEvent>();
                events.AddRange(tg.GetEventType<Tap>());
                events.AddRange(tg.GetEventType<Hold>());
                events.AddRange(tg.GetEventType<Arc>());
                events.AddRange(tg.GetEventType<TimingEvent>());
                events.AddRange(Services.Gameplay.Chart.GetAll<CameraEvent>().Where(cam => cam.TimingGroup == tg.GroupNumber));
                events.AddRange(Services.Gameplay.Chart.GetAll<ScenecontrolEvent>().Where(sc => sc.TimingGroup == tg.GroupNumber));
                if (tg.ReferenceEvents != null)
                {
                    events.AddRange(tg.ReferenceEvents);
                }

                events.Sort(
                    (a, b) =>
                    {
                        // If timings differ, sort by timing
                        if(a.Timing != b.Timing)
                        {
                            return a.Timing.CompareTo(b.Timing);
                        }

                        // Otherwise, if importance differs, sort by importance
                        int atype = GetImportance(a);
                        int btype = GetImportance(b);
                        if(atype != btype)
                        {
                            return atype.CompareTo(btype);
                        }

                        // Finally, compare directly
                        switch(a)
                        {
                            // Timing events; cannot have two events at the same time, so always return 0
                            case TimingEvent:
                                return 0;
                            
                            // Tap; compare by lane
                            case Tap:
                                Tap atap = a as Tap;
                                Tap btap = b as Tap;

                                if(atap.Lane != btap.Lane)
                                {
                                    return atap.Lane.CompareTo(btap.Lane);
                                }

                                return 0;

                            // Hold; compare by lane then end time
                            case Hold:
                                Hold ahold = a as Hold;
                                Hold bhold = b as Hold;

                                if(ahold.Lane != ahold.Lane)
                                {
                                    return ahold.Lane.CompareTo(ahold.Lane);
                                }

                                if(ahold.EndTiming != bhold.EndTiming)
                                {
                                    return ahold.EndTiming.CompareTo(bhold.EndTiming);
                                }

                                return 0;

                            // Arc; compare by end time -> x0 -> y0 -> kind -> x1 -> y1 -> isTrace -> color -> sfx
                            case Arc:
                                Arc aarc = a as Arc;
                                Arc barc = b as Arc;

                                if(aarc.EndTiming != barc.EndTiming)
                                {
                                    return aarc.EndTiming.CompareTo(barc.EndTiming);
                                }

                                if(aarc.XStart != barc.XStart)
                                {
                                    return aarc.XStart.CompareTo(barc.XStart);
                                }
                                
                                if(aarc.YStart != barc.YStart)
                                {
                                    return aarc.YStart.CompareTo(barc.YStart);
                                }

                                if(aarc.LineType != barc.LineType)
                                {
                                    return aarc.LineType.CompareTo(barc.LineType);
                                }
                                
                                if(aarc.XEnd != barc.XEnd)
                                {
                                    return aarc.XEnd.CompareTo(barc.XEnd);
                                }
                                
                                if(aarc.YEnd != barc.YEnd)
                                {
                                    return aarc.YEnd.CompareTo(barc.YEnd);
                                }

                                if(aarc.IsTrace != barc.IsTrace)
                                {
                                    return aarc.IsTrace.CompareTo(barc.IsTrace);
                                }

                                if(aarc.Color != barc.Color)
                                {
                                    return aarc.Color.CompareTo(barc.Color);
                                }

                                if(aarc.Sfx != barc.Sfx)
                                {
                                    return aarc.Sfx.CompareTo(barc.Sfx);
                                }

                                return 0;

                            // Camera event; sort by duration -> type -> move -> rotate
                            case CameraEvent:
                                CameraEvent acam = a as CameraEvent;
                                CameraEvent bcam = b as CameraEvent;

                                if(acam.Duration != bcam.Duration)
                                {
                                    return acam.Duration.CompareTo(bcam.Duration);
                                }

                                if(acam.CameraType != bcam.CameraType)
                                {
                                    return acam.CameraType.CompareTo(bcam.CameraType);
                                }

                                if(!acam.Move.Equals(bcam.Move))
                                {
                                    return CompareVec3(acam.Move, bcam.Move);
                                }

                                if(!acam.Rotate.Equals(bcam.Rotate))
                                {
                                    return CompareVec3(acam.Rotate, bcam.Rotate);
                                }

                                return 0;

                            // Scenecontrol event; sort by type then arguments in order
                            case ScenecontrolEvent:
                                ScenecontrolEvent ascene = a as ScenecontrolEvent;
                                ScenecontrolEvent bscene = b as ScenecontrolEvent;

                                if(ascene.Typename != bscene.Typename)
                                {
                                    return ascene.Typename.CompareTo(bscene.Typename);
                                }

                                if(ascene.Arguments.Count != bscene.Arguments.Count)
                                {
                                    return ascene.Arguments.Count.CompareTo(bscene.Arguments.Count);
                                }

                                for(int i = 0; i < ascene.Arguments.Count; i++)
                                {
                                    object aarg = ascene.Arguments[i];
                                    object barg = bscene.Arguments[i];

                                    int aargimp = GetScenecontrolArgImportance(aarg);
                                    int bargimp = GetScenecontrolArgImportance(barg);

                                    if(aargimp != bargimp)
                                    {
                                        return aargimp.CompareTo(bargimp);
                                    }

                                    switch(aarg)
                                    {
                                        case float:
                                            float afloat = (float)aarg;
                                            float bfloat = (float)barg;

                                            if(afloat != bfloat)
                                            {
                                                return afloat.CompareTo(bfloat);
                                            }
                                            break;

                                        case string:
                                            string astr = (string)aarg;
                                            string bstr = (string)barg;

                                            if(astr != bstr)
                                            {
                                                return astr.CompareTo(bstr);
                                            }
                                            break;

                                        // Note: other argument types are not currently possible,
                                        // so they will be treated as though they are equivalent
                                    }
                                }

                                return 0;

                            // Include event -> sort by file
                            case IncludeEvent:
                                IncludeEvent aincl = a as IncludeEvent;
                                IncludeEvent bincl = b as IncludeEvent;

                                if(aincl.File != bincl.File)
                                {
                                    return aincl.File.CompareTo(bincl.File);
                                }

                                return 0;

                            // Fragment event -> sort by file
                            case FragmentEvent:
                                FragmentEvent afrag = a as FragmentEvent;
                                FragmentEvent bfrag = b as FragmentEvent;

                                if(afrag.File != bfrag.File)
                                {
                                    return afrag.File.CompareTo(bfrag.File);
                                }

                                return 0;

                            // Default; throw a not supported exception to fail fast
                            default:
                                throw new NotSupportedException();
                        }
                    });

                IEnumerable<RawEvent> rawevents = events.Select<ArcEvent, RawEvent>(
                    (ArcEvent ev) =>
                    {
                        switch (ev)
                        {
                            case TimingEvent timing:
                                return new RawTiming
                                {
                                    Type = RawEventType.Timing,
                                    Timing = timing.Timing,
                                    TimingGroup = timing.TimingGroup,
                                    Bpm = timing.Bpm,
                                    Divisor = timing.Divisor,
                                };
                            case Tap tap:
                                return new RawTap
                                {
                                    Type = RawEventType.Tap,
                                    Timing = tap.Timing,
                                    TimingGroup = tap.TimingGroup,
                                    Lane = tap.Lane,
                                };
                            case Hold hold:
                                return new RawHold
                                {
                                    Type = RawEventType.Hold,
                                    Timing = hold.Timing,
                                    EndTiming = hold.EndTiming,
                                    TimingGroup = hold.TimingGroup,
                                    Lane = hold.Lane,
                                };
                            case Arc arc:
                                var ats = Services.Gameplay.Chart
                                    .GetAll<ArcTap>()
                                    .Where(at => at.Arc == arc)
                                    .OrderBy(at => at.Timing)
                                    .ThenBy(at => at.Width)
                                    .Select(at => new RawArcTap
                                    {
                                        Type = RawEventType.ArcTap,
                                        Timing = at.Timing,
                                        TimingGroup = arc.TimingGroup,
                                        Width = at.Width,
                                    });
                                return new RawArc
                                {
                                    Type = RawEventType.Arc,
                                    Timing = arc.Timing,
                                    EndTiming = arc.EndTiming,
                                    TimingGroup = arc.TimingGroup,
                                    Color = arc.Color,
                                    IsTrace = arc.IsTrace,
                                    LineType = arc.LineType.ToLineTypeString(),
                                    XEnd = arc.XEnd,
                                    XStart = arc.XStart,
                                    YEnd = arc.YEnd,
                                    YStart = arc.YStart,
                                    Sfx = arc.Sfx,
                                    ArcTaps = ats.ToList(),
                                };
                            case CameraEvent cam:
                                return new RawCamera
                                {
                                    Type = RawEventType.Camera,
                                    TimingGroup = cam.TimingGroup,
                                    Timing = cam.Timing,
                                    Move = cam.Move,
                                    Rotate = cam.Rotate,
                                    CameraType = cam.CameraType.ToCameraString(),
                                    Duration = cam.Duration,
                                };
                            case ScenecontrolEvent sc:
                                return new RawSceneControl()
                                {
                                    Type = RawEventType.SceneControl,
                                    Timing = sc.Timing,
                                    TimingGroup = sc.TimingGroup,
                                    SceneControlTypeName = sc.Typename,
                                    Arguments = sc.Arguments,
                                };
                            case IncludeEvent incl:
                                return new RawInclude()
                                {
                                    Type = RawEventType.Include,
                                    Timing = incl.Timing,
                                    TimingGroup = incl.TimingGroup,
                                    File = incl.File,
                                };
                            case FragmentEvent frag:
                                return new RawFragment()
                                {
                                    Type = RawEventType.Fragment,
                                    Timing = frag.Timing,
                                    TimingGroup = frag.TimingGroup,
                                    File = frag.File,
                                };
                            default:
                                return null;
                        }
                    }).ToList();

                list.Add((rawprop, rawevents));
            }

            // Sort by timing group id number
            list.Sort((a, b) => rawMap[a.groups].GroupNumber.CompareTo(rawMap[b.groups].GroupNumber));
            return list;
        }

        private int CompareVec3(Vector3 a, Vector3 b)
        {
            if(a.x != b.x)
            {
                return a.x.CompareTo(b.x);
            }

            if(a.y != b.y)
            {
                return a.y.CompareTo(b.y);
            }

            if(a.z != b.z)
            {
                return a.z.CompareTo(b.z);
            }

            return 0;
        }

        private int GetScenecontrolArgImportance(object o)
        {
            switch(o)
            {
                case float:
                    return 0;
                case string:
                    return 1;
                default:
                    return 2;
            }
        }

        private int GetImportance(ArcEvent a)
        {
            switch (a)
            {
                case TimingEvent time:
                    return 0;
                case IncludeEvent incl:
                    return 1;
                case FragmentEvent frag:
                    return 2;
                case Tap tap:
                    return 3;
                case Hold hold:
                    return 4;
                case Arc arc:
                    return 5;
                case CameraEvent cam:
                    return 6;
                case ScenecontrolEvent sc:
                    return 7;
                default:
                    return 8;
            }
        }
    }
}