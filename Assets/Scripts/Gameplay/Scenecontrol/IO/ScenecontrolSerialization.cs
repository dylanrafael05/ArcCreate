using System;
using System.Collections.Generic;

namespace ArcCreate.Gameplay.Scenecontrol
{
    public class ScenecontrolSerialization
    {
        private readonly List<ISerializableUnit> units = new List<ISerializableUnit>();
        private readonly List<SerializedUnit> serializedUnits = new List<SerializedUnit>();
        private readonly Dictionary<ISerializableUnit, int> idLookup = new Dictionary<ISerializableUnit, int>();

        public List<SerializedUnit> Result => serializedUnits;

        public int? AddUnitAndGetId(ISerializableUnit unit)
        {
            if (unit == null)
            {
                return null;
            }

            if (idLookup.TryGetValue(unit, out int id))
            {
                return id;
            }

            units.Add(unit);
            id = units.Count - 1;
            idLookup.Add(unit, id);
            SerializedUnit serialized = default;
            serializedUnits.Add(serialized);
            serializedUnits[id] = new SerializedUnit
            {
                Type = GetTypeFromUnit(unit),
                Properties = unit.SerializeProperties(this),
            };

            return id;
        }

        public string GetTypeFromUnit(ISerializableUnit unit)
        {
            switch (unit)
            {
                case Context context:
                    return "context";

                // Channels
                case KeyChannel key:
                    return "channel.key";
                case FFTChannel fft:
                    return "channel.fft";
                case ClampChannel clamp:
                    return "channel.clamp";
                case ConditionalChannel condition:
                    return "channel.condition";
                case ConstantChannel constant:
                    return "channel.const";
                case CosChannel cos:
                    return "channel.cos";
                case ExpChannel exp:
                    return "channel.exp";
                case InverseChannel inverse:
                    return "channel.inverse";
                case MaxChannel max:
                    return "channel.max";
                case MinChannel min:
                    return "channel.min";
                case NegateChannel negate:
                    return "channel.negate";
                case NoiseChannel noise:
                    return "channel.noise";
                case ProductChannel product:
                    return "channel.product";
                case RandomChannel random:
                    return "channel.random";
                case SawChannel saw:
                    return "channel.saw";
                case SineChannel sine:
                    return "channel.sine";
                case SumChannel sum:
                    return "channel.sum";
                case TimingChannel timing:
                    return "channel.timing";
                case AbsChannel abs:
                    return "channel.abs";
                case TimeScaleChannel ts:
                    return "channel.time.scale";
                case TimeShiftChannel tsh:
                    return "channel.time.shift";
                case PureSineChannel ps:
                    return "channel.time.chain";
                case ChainChannel chain:
                    return "channel.puresine";
                case PureCosChannel pc:
                    return "channel.purecos";
                case ModuloChannel pc:
                    return "channel.modulo";
                case IfElseChannel ie:
                    return "channel.ifelse";

                // Note channels
                case NoteTimingChannel nTiming:
                    return "channel.note.timing";
                case NoteFloorPositionChannel nFP:
                    return "channel.note.floorpos";
                case NoteXPositionChannel nx:
                    return "channel.note.x";
                case NoteYPositionChannel ny:
                    return "channel.note.y";
                case NoteZPositionChannel nz:
                    return "channel.note.z";
                case NoteIDChannel id:
                    return "channel.note.id";
                case NoteIsArcChannel arc:
                    return "channel.note.isarc";
                case NoteTypeChannel type:
                    return "channel.note.type";

                // Trigger channels
                case AccumulatingTriggerChannel accum:
                    return "channel.trigger.accumulate";
                case LoopingTriggerChannel loop:
                    return "channel.trigger.loop";
                case StackingTriggerChannel stack:
                    return "channel.trigger.stack";
                case SettingTriggerChannel setting:
                    return "channel.trigger.set";

                // String channels
                case KeyStringChannel keystring:
                    return "channel.string.key";
                case ConstantTextChannel constanttext:
                    return "channel.text.constant";
                case KeyTextChannel keytext:
                    return "channel.text.key";
                case ConcatTextChannel concat:
                    return "channel.text.concat";
                case ValueToTextChannel valuetext:
                    return "channel.text.value";

                // Boolean channels
                case BooleanConstantChannel bconst:
                    return "channel.bool.constant";
                case NotChannel not:
                    return "channel.bool.not";
                case AndChannel and:
                    return "channel.bool.and";
                case OrChannel or:
                    return "channel.bool.or";
                case NumericalComparisonChannel ncomp:
                    return "channel.bool.comp.num";
                case StringComparisonChannel scomp:
                    return "channel.bool.comp.str";

                // Contexts
                case DropRateChannel droprate:
                    return "channel.context.droprate";
                case GlobalOffsetChannel goffset:
                    return "channel.context.globaloffset";
                case CurrentScoreChannel score:
                    return "channel.context.currentscore";
                case CurrentComboChannel combo:
                    return "channel.context.currentcombo";
                case CurrentTimingChannel timing:
                    return "channel.context.currenttiming";
                case ScreenWidthChannel swidth:
                    return "channel.context.screenwidth";
                case ScreenHeightChannel sheight:
                    return "channel.context.screenheight";
                case ScreenIs16By9Channel is16by9:
                    return "channel.context.is16by9";
                case BPMChannel bpm:
                    return "channel.context.bpm";
                case DivisorChannel divisor:
                    return "channel.context.divisor";
                case FloorPositionChannel fp:
                    return "channel.context.floorposition";

                // Triggers
                case JudgementTrigger tjudgement:
                    return "trigger.judgement";
                case ObserveTrigger tobserve:
                    return "trigger.observe";
                default:

                    if (unit is ISceneController controller)
                    {
                        string name = controller?.SerializedType;
                        if (!string.IsNullOrEmpty(name))
                        {
                            return name;
                        }
                    }

                    throw new Exception($"Could not get type of object: {unit.GetType().Name}");
            }
        }
    }
}