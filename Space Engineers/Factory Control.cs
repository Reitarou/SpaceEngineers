using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Ingame;
using Sandbox.Game.EntityComponents;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRage;
using VRageMath;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game.ModAPI.Ingame;
using System.Linq;

namespace SpaceEngineers.FactoryControl
{
    public sealed class Program : MyGridProgram
    {
        private class MyItemTypeComparer : IComparer<MyItemType>
        {
            public int Compare(MyItemType x, MyItemType y)
            {
                return GetTypeSortorder(x).CompareTo(GetTypeSortorder(y));
            }

            private int GetTypeSortorder(MyItemType itemType)
            {
                var sortOrder = int.MaxValue;
                switch (itemType.TypeId)
                {
                    case TypeIdOres:
                        sortOrder = 0;
                        break;

                    case TypeIdIngots:
                        sortOrder = 1000;
                        switch (itemType.SubtypeId)
                        {
                            case SubtypeIronIngot:
                                sortOrder += 1;
                                break;

                            case SubtypeNickelIngot:
                                sortOrder += 2;
                                break;

                            case SubtypeSiliconIngot:
                                sortOrder += 3;
                                break;

                            case SubtypeCobaltIngot:
                                sortOrder += 4;
                                break;

                            case SubtypeSilverIngot:
                                sortOrder += 5;
                                break;

                            case SubtypeStoneIngot:
                                sortOrder += 10;
                                break;

                            default:
                                sortOrder += 100;
                                break;
                        }
                        break;

                    case TypeIdComponents:
                        sortOrder = 10000;
                        switch (itemType.SubtypeId)
                        {
                            case SubTypeIdSteelPlate:
                                sortOrder += 1;
                                break;

                            case SubTypeIdInteriorPlate:
                                sortOrder += 2;
                                break;

                            case SubTypeIdConstructionComponent:
                                sortOrder += 3;
                                break;

                            default:
                                sortOrder += 100;
                                break;
                        }
                        break;
                }

                return sortOrder;
            }
        }

        const string TypeIdOres = "MyObjectBuilder_Ore";

        const string TypeIdIngots = "MyObjectBuilder_Ingot";
        const string SubtypeIronIngot = "Iron";
        const string SubtypeNickelIngot = "Nickel";
        const string SubtypeSiliconIngot = "Silicon";
        const string SubtypeCobaltIngot = "Cobalt";
        const string SubtypeSilverIngot = "Silver";
        const string SubtypeStoneIngot = "Stone";
        static readonly MyItemType TypeIron = new MyItemType(TypeIdIngots, SubtypeIronIngot);
        static readonly MyItemType TypeNickel = new MyItemType(TypeIdIngots, SubtypeNickelIngot);
        static readonly MyItemType TypeSilicon = new MyItemType(TypeIdIngots, SubtypeSiliconIngot);
        static readonly MyItemType TypeCobalt = new MyItemType(TypeIdIngots, SubtypeCobaltIngot);

        const string TypeIdComponents = "MyObjectBuilder_Component";
        const string SubTypeIdSteelPlate = "SteelPlate";
        const string SubTypeIdInteriorPlate = "InteriorPlate";
        const string SubTypeIdConstructionComponent = "Construction";
        const string SubTypeIdComputer = "Computer";
        const string SubTypeIdMotor = "Motor";
        const string SubTypeIdMetalGrid = "MetalGrid";
        const string SubTypeIdPowerCell = "PowerCell";
        const string SubTypeIdLargeTube = "LargeTube";
        const string SubTypeIdSmallTube = "SmallTube";
        const string SubTypeIdDetector = "Detector";
        const string SubTypeIdDisplay = "Display";
        const string SubTypeIdRadioCommunication = "RadioCommunication";

        static readonly MyItemType TypeSteelPlate = new MyItemType(TypeIdComponents, SubTypeIdSteelPlate);
        static readonly MyItemType TypeInteriorPlate = new MyItemType(TypeIdComponents, SubTypeIdInteriorPlate);
        static readonly MyItemType TypeConstructionComponent = new MyItemType(TypeIdComponents, SubTypeIdConstructionComponent);
        static readonly MyItemType TypeComputer = new MyItemType(TypeIdComponents, SubTypeIdComputer);
        static readonly MyItemType TypeMotor = new MyItemType(TypeIdComponents, SubTypeIdMotor);
        static readonly MyItemType TypeMetalGrid = new MyItemType(TypeIdComponents, SubTypeIdMetalGrid);
        static readonly MyItemType TypePowerCell = new MyItemType(TypeIdComponents, SubTypeIdPowerCell);
        static readonly MyItemType TypeLargeTube = new MyItemType(TypeIdComponents, SubTypeIdLargeTube);
        static readonly MyItemType TypeSmallTube = new MyItemType(TypeIdComponents, SubTypeIdSmallTube);
        static readonly MyItemType TypeDetector = new MyItemType(TypeIdComponents, SubTypeIdDetector);
        static readonly MyItemType TypeDisplay = new MyItemType(TypeIdComponents, SubTypeIdDisplay);
        static readonly MyItemType TypeRadioCommunication = new MyItemType(TypeIdComponents, SubTypeIdRadioCommunication);

        static readonly MyFixedPoint MinIngotsInAssemblies = new MyFixedPoint() { RawValue = 100000000 };

        IMyCargoContainer m_MainCargoContainer;
        IMyAssembler m_MainAssembler;
        List<IMyAssembler> m_Assemblers = new List<IMyAssembler>();
        List<IMyRefinery> m_Refineries = new List<IMyRefinery>();

        IMyTextSurface m_FrontLcdOres;
        IMyTextSurface m_FrontLcdComponents;
        IMyTextSurface m_FrontLcdItems;

        List<KeyValuePair<IMyTerminalBlock, IMySlimBlock>> m_AllBlocks = new List<KeyValuePair<IMyTerminalBlock, IMySlimBlock>>();

        MyItemTypeComparer m_ItemComparer = new MyItemTypeComparer();

        private string m_ItemBlueprintSubtype = string.Empty;

        public Program()
        {
            m_MainCargoContainer = GridTerminalSystem.GetBlockWithName("Alpha Main Cargo") as IMyCargoContainer;
            GridTerminalSystem.GetBlocksOfType(m_Assemblers, a => a is IMyAssembler);
            m_MainAssembler = m_Assemblers.First(a => a.CustomName == "Assembler Main");
            GridTerminalSystem.GetBlocksOfType(m_Refineries, a => a is IMyRefinery);

            m_FrontLcdOres = GridTerminalSystem.GetBlockWithName("Alpha LCD Ores Ingots") as IMyTextSurface;
            m_FrontLcdComponents = GridTerminalSystem.GetBlockWithName("Alpha LCD Components") as IMyTextSurface;
            m_FrontLcdItems = GridTerminalSystem.GetBlockWithName("Alpha LCD Items") as IMyTextSurface;

            var allBlocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(allBlocks);
            foreach (var block in allBlocks)
                m_AllBlocks.Add(new KeyValuePair<IMyTerminalBlock, IMySlimBlock>(block, block.CubeGrid.GetCubeBlock(block.Position)));

            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main(string args)
        {
            var mainInventory = m_MainCargoContainer.GetInventory(0);

            //Сбор произведённого из ассемблеров и рефайнери
            foreach (var assembler in m_Assemblers)
            {
                var inventory = assembler.GetInventory(1);
                while (inventory.ItemCount != 0)
                {
                    mainInventory.TransferItemFrom(inventory, 0, null, true);
                }
            }
            foreach (var refinery in m_Refineries)
            {
                var inventory = refinery.GetInventory(1);
                while (inventory.ItemCount != 0)
                {
                    mainInventory.TransferItemFrom(inventory, 0, null, true);
                }
            }

            //Раздача слитков по ассемблерам
            AddIngotsToAssemblers();

            //Подсчёт всех ресурсов на базе
            var resourcesOnFactory = new Dictionary<MyItemType, MyFixedPoint>();
            SummaryItemsOnFactory(resourcesOnFactory);

            //Заказ в ассемблерах
            OrderInAssemblers(resourcesOnFactory);
        }

        private void OrderInAssemblers(Dictionary<MyItemType, MyFixedPoint> resourcesOnFactory)
        {
            var steelPlates = TypeSteelPlate;
            OrderItemType(resourcesOnFactory, steelPlates, 20000);
            var otherItems = new List<MyItemType>();
            otherItems.Add(TypeInteriorPlate);
            otherItems.Add(TypeConstructionComponent);
            otherItems.Add(TypeComputer);
            otherItems.Add(TypeMotor);
            otherItems.Add(TypeMetalGrid);
            otherItems.Add(TypePowerCell);
            otherItems.Add(TypeLargeTube);
            otherItems.Add(TypeSmallTube);
            otherItems.Add(TypeDetector);
            otherItems.Add(TypeDisplay);
            otherItems.Add(TypeRadioCommunication);

            foreach (var item in otherItems)
            {
                OrderItemType(resourcesOnFactory, item, 1000);
            }

            //if (m_ItemBlueprintSubtype == string.Empty)
            //{
            //    if (!m_MainAssembler.IsQueueEmpty)
            //    {
            //        var list = new List<MyProductionItem>();
            //        m_MainAssembler.GetQueue(list);
            //        m_ItemBlueprintSubtype = list.First().BlueprintId.SubtypeId.ToString();
            //    }
            //}
            //Echo(m_ItemBlueprintSubtype);
        }

        private string m_LastOrder = string.Empty;
        private void OrderItemType(Dictionary<MyItemType, MyFixedPoint> resourcesOnFactory, MyItemType item, int maxCount)
        {
            int orderCount = maxCount;
            if (resourcesOnFactory.ContainsKey(item))
            {
                orderCount = maxCount - resourcesOnFactory[item].ToIntSafe();
            }
            if (orderCount > 0)
            {
                var blueprintDef = GetDefId(GetBlueprintName(item));
                Echo("BlueprintDef = " + blueprintDef.SubtypeId.ToString());
                var canUse = m_MainAssembler.CanUseBlueprint(blueprintDef);
                var queueEmpty = true;
                foreach (var assembler in m_Assemblers)
                    if (!assembler.IsQueueEmpty)
                        queueEmpty = false;
                Echo(canUse.ToString());
                if (canUse && queueEmpty)
                {
                    m_LastOrder = string.Format("{0}: {1}/{2} at{3}", blueprintDef.SubtypeId.ToString(), orderCount, maxCount, DateTime.Now);
                    Echo("AddToQueue " + blueprintDef.SubtypeId.ToString());
                    m_MainAssembler.AddQueueItem(blueprintDef, (decimal)(m_Assemblers.Count * 10));
                }
            }
            Echo(m_LastOrder);
        }

        private string GetBlueprintName(MyItemType itemType)
        {
            switch (itemType.TypeId)
            {
                case TypeIdComponents:
                    switch (itemType.SubtypeId)
                    {
                        case SubTypeIdComputer:
                        case SubTypeIdDetector:
                        case SubTypeIdMotor:
                        case SubTypeIdRadioCommunication:
                            return string.Format("{0}Component", itemType.SubtypeId);
                    }
                    break;
            }
            return itemType.SubtypeId;
        }

        private MyDefinitionId GetDefId(string subtypeId)
        {
            return MyDefinitionId.Parse(string.Format("{0}/{1}", "MyObjectBuilder_BlueprintDefinition", subtypeId));
        }

        private void SummaryItemsOnFactory(Dictionary<MyItemType, MyFixedPoint> resourcesOnFactory)
        {
            AddItemsToDictionary(resourcesOnFactory, m_MainCargoContainer);

            foreach (var refinery in m_Refineries)
            {
                AddItemsToDictionary(resourcesOnFactory, refinery);
            }

            foreach (var assembler in m_Assemblers)
            {
                AddItemsToDictionary(resourcesOnFactory, assembler);
            }

            var ores = new List<KeyValuePair<MyItemType, MyFixedPoint>>();
            var ingots = new List<KeyValuePair<MyItemType, MyFixedPoint>>();
            var components = new List<KeyValuePair<MyItemType, MyFixedPoint>>();
            var otherItems = new List<KeyValuePair<MyItemType, MyFixedPoint>>();

            foreach (var pair in resourcesOnFactory)
            {
                switch (pair.Key.TypeId)
                {
                    case TypeIdOres:
                        ores.Add(pair);
                        break;

                    case TypeIdIngots:
                        ingots.Add(pair);
                        break;

                    case TypeIdComponents:
                        components.Add(pair);
                        break;

                    default:
                        otherItems.Add(pair);
                        break;
                }
            }

            ores.Sort((a, b) => m_ItemComparer.Compare(a.Key, b.Key));
            ingots.Sort((a, b) => m_ItemComparer.Compare(a.Key, b.Key));
            components.Sort((a, b) => m_ItemComparer.Compare(a.Key, b.Key));

            string textString = "=== ORES ===\n";
            foreach (var pair in ores)
            {
                textString += AddItemNameAmountString(pair.Key, pair.Value);
            }
            textString += "\n=== INGOTS ===\n";
            foreach (var pair in ingots)
            {
                textString += AddItemNameAmountString(pair.Key, pair.Value);
            }
            m_FrontLcdOres.WriteText(textString);

            textString = "=== COMPONENTS ===\n";
            foreach (var pair in components)
            {
                textString += AddItemNameAmountString(pair.Key, pair.Value);
            }
            m_FrontLcdComponents.WriteText(textString);

            textString = "=== ITEMS ===\n";
            foreach (var pair in otherItems)
            {
                textString += AddItemNameAmountString(pair.Key, pair.Value);
            }

            textString += "\n=== INTEGRITY ===\n";
            var damagedBlocks = new List<IMyTerminalBlock>();
            foreach (var pair in m_AllBlocks)
            {
                if (!pair.Value.IsFullIntegrity)
                {
                    damagedBlocks.Add(pair.Key);
                }
            }
            if (damagedBlocks.Count == 0)
                textString += "All blocks undamaged! ;)\n";
            else
            {
                textString += "!!! Damaged Blocks !!!\n";
                foreach (var block in damagedBlocks)
                {
                    block.ShowOnHUD = true;
                    textString += string.Format("{0}: {1}\n", block.CubeGrid.DisplayName, block.CustomName);
                }
            }
            m_FrontLcdItems.WriteText(textString);
        }

        private static void AddItemsToDictionary(Dictionary<MyItemType, MyFixedPoint> resourcesOnFactory, IMyTerminalBlock block)
        {
            var items = new List<MyInventoryItem>();
            for (int i = 0; i < block.InventoryCount; i++)
            {
                var inventory = block.GetInventory(i);
                inventory.GetItems(items);
            }
            foreach (var item in items)
            {
                if (!resourcesOnFactory.ContainsKey(item.Type))
                    resourcesOnFactory[item.Type] = item.Amount;
                else
                    resourcesOnFactory[item.Type] += item.Amount;
            }
        }

        private string AddItemNameAmountString(MyItemType itemType, MyFixedPoint itemAmount)
        {
            string name, amount;
            switch (itemType.TypeId)
            {
                case TypeIdOres:
                    switch (itemType.SubtypeId)
                    {
                        default:
                            name = itemType.SubtypeId;
                            break;
                    }
                    break;

                case TypeIdIngots:
                    switch (itemType.SubtypeId)
                    {
                        case SubtypeIronIngot:
                            name = "Iron Ingots";
                            break;

                        case SubtypeNickelIngot:
                            name = "Nickel Ingots";
                            break;

                        case SubtypeSiliconIngot:
                            name = "Silicon Ingots";
                            break;

                        case SubtypeCobaltIngot:
                            name = "Cobalt Ingots";
                            break;

                        case SubtypeSilverIngot:
                            name = "Silver Ingots";
                            break;

                        case SubtypeStoneIngot:
                            name = "Gravel";
                            break;

                        default:
                            name = itemType.SubtypeId;
                            break;
                    }
                    break;

                case TypeIdComponents:
                    switch (itemType.SubtypeId)
                    {
                        default:
                            name = itemType.SubtypeId;
                            break;
                    }
                    break;

                default:
                    name = itemType.SubtypeId;
                    break;
            }

            long Million = 1000000000000;
            long Thousand = 1000000000;
            long Zero = 1000000;
            if (itemAmount.RawValue > Million)
                amount = (itemAmount.RawValue / Million).ToString() + " M";
            else if (itemAmount.RawValue > Thousand)
                amount = (itemAmount.RawValue / Thousand).ToString() + " K";
            else
                amount = (itemAmount.RawValue / Zero).ToString();

            return string.Format("{0}: {1}\n", name, amount);
        }

        private void AddIngotsToAssemblers()
        {
            var mainInventory = m_MainCargoContainer.GetInventory(0);
            var mainItems = new List<MyInventoryItem>();
            mainInventory.GetItems(mainItems, a => a.Type.TypeId == TypeIdIngots);

            var resList = new List<KeyValuePair<MyItemType, MyInventoryItem>>();
            resList.Add(new KeyValuePair<MyItemType, MyInventoryItem>(TypeIron, mainItems.Find(a => a.Type == TypeIron)));
            resList.Add(new KeyValuePair<MyItemType, MyInventoryItem>(TypeNickel, mainItems.Find(a => a.Type == TypeNickel)));
            resList.Add(new KeyValuePair<MyItemType, MyInventoryItem>(TypeSilicon, mainItems.Find(a => a.Type == TypeSilicon)));
            resList.Add(new KeyValuePair<MyItemType, MyInventoryItem>(TypeCobalt, mainItems.Find(a => a.Type == TypeCobalt)));

            //Раздача слитков по ассемблерам
            foreach (var assembler in m_Assemblers)
            {
                var inventory = assembler.GetInventory(0);
                foreach (var pair in resList)
                {
                    if (pair.Value != null)
                    {
                        var amount = inventory.GetItemAmount(pair.Key);
                        if (amount < MinIngotsInAssemblies)
                            inventory.TransferItemFrom(mainInventory, pair.Value, MinIngotsInAssemblies - amount);
                    }
                }
            }
        }

        public void Save()
        { }
    }
}