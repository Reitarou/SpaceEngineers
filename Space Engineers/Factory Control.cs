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
        const string MainCargoName = "Factory Main Cargo";
        const string ToolsCargoName = "Factory Tools Cargo";
        const string MainAssemblerName = "Factory Assembler Main";
        const string MainGasGeneratorName = "Factory O2/H2 Generator Main";
        const string OresIngotsLcdName = "Factory LCD Ores Ingots";
        const string ComponentsLcdName = "Factory LCD Components";

        const int SteelPlatesOrderTarget = 20000;
        const int OtherComponentsOrderTarget = 1000;
        const int IngotsInAssemblies = 100;

        private class MyItemTypeComparer : IComparer<MyItemType>
        {
            public int Compare(MyItemType x, MyItemType y)
            {
                return GetTypeSortorder(x).CompareTo(GetTypeSortorder(y));
            }

            public static int GetTypeSortorder(MyItemType itemType)
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
                            case SubTypeIdIronIngot:
                                sortOrder += 1;
                                break;

                            case SubTypeIdNickelIngot:
                                sortOrder += 2;
                                break;

                            case SubTypeIdSiliconIngot:
                                sortOrder += 3;
                                break;

                            case SubTypeIdCobaltIngot:
                                sortOrder += 4;
                                break;

                            case SubTypeIdSilverIngot:
                                sortOrder += 5;
                                break;

                            case SubTypeIdStoneIngot:
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

                            case SubTypeIdComputer:
                                sortOrder += 4;
                                break;

                            case SubTypeIdMotor:
                                sortOrder += 5;
                                break;

                            case SubTypeIdMetalGrid:
                                sortOrder += 6;
                                break;

                            case SubTypeIdSmallTube:
                                sortOrder += 7;
                                break;

                            case SubTypeIdLargeTube:
                                sortOrder += 8;
                                break;

                            case SubTypeIdDisplay:
                                sortOrder += 9;
                                break;

                            case SubTypeIdGirder:
                                sortOrder += 10;
                                break;

                            case SubTypeIdDetector:
                                sortOrder += 11;
                                break;

                            case SubTypeIdRadioCommunication:
                                sortOrder += 12;
                                break;

                            case SubTypeIdBulletproofBlass:
                                sortOrder += 13;
                                break;

                            case SubTypeIdPowerCell:
                                sortOrder += 14;
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

        #region TypeIds

        const string TypeIdOres = "MyObjectBuilder_Ore";

        const string TypeIdIngots = "MyObjectBuilder_Ingot";
        const string SubTypeIdIronIngot = "Iron";
        const string SubTypeIdNickelIngot = "Nickel";
        const string SubTypeIdSiliconIngot = "Silicon";
        const string SubTypeIdCobaltIngot = "Cobalt";
        const string SubTypeIdSilverIngot = "Silver";
        const string SubTypeIdStoneIngot = "Stone";
        static readonly MyItemType TypeIron = new MyItemType(TypeIdIngots, SubTypeIdIronIngot);
        static readonly MyItemType TypeNickel = new MyItemType(TypeIdIngots, SubTypeIdNickelIngot);
        static readonly MyItemType TypeSilicon = new MyItemType(TypeIdIngots, SubTypeIdSiliconIngot);
        static readonly MyItemType TypeCobalt = new MyItemType(TypeIdIngots, SubTypeIdCobaltIngot);

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
        const string SubTypeIdGirder = "Girder";
        const string SubTypeIdBulletproofBlass = "BulletproofGlass";

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
        static readonly MyItemType TypeGirder = new MyItemType(TypeIdComponents, SubTypeIdGirder);
        static readonly MyItemType TypeBulletproofGlass = new MyItemType(TypeIdComponents, SubTypeIdBulletproofBlass);

        const string SubTypeIdHydrogenBottle = "HydrogenBottle";
        const string SubTypeIdOxygenBottle = "OxygenBottle";

        #endregion

        static readonly MyFixedPoint MinIngotsInAssemblies = new MyFixedPoint() { RawValue = IngotsInAssemblies * 1000000 };

        IMyCargoContainer m_MainCargoContainer;
        IMyCargoContainer m_ToolsCargoContainer;

        IMyAssembler m_MainAssembler;
        IMyGasGenerator m_MainGasGenerator;

        List<IMyAssembler> m_Assemblers = new List<IMyAssembler>();
        List<IMyRefinery> m_Refineries = new List<IMyRefinery>();

        IMyTextSurface m_FrontLcdOres;
        IMyTextSurface m_FrontLcdComponents;

        MyItemTypeComparer m_ItemComparer = new MyItemTypeComparer();

        string m_ItemBlueprintSubtype = string.Empty;
        List<MyItemType> m_OrderList;

        public Program()
        {
            m_MainCargoContainer = GridTerminalSystem.GetBlockWithName(MainCargoName) as IMyCargoContainer;
            m_ToolsCargoContainer = GridTerminalSystem.GetBlockWithName(ToolsCargoName) as IMyCargoContainer;

            GridTerminalSystem.GetBlocksOfType(m_Assemblers, a => (a is IMyAssembler && a.CubeGrid == m_MainCargoContainer.CubeGrid));
            GridTerminalSystem.GetBlocksOfType(m_Refineries, a => (a is IMyRefinery && a.CubeGrid == m_MainCargoContainer.CubeGrid));

            m_MainAssembler = m_Assemblers.First(a => a.CustomName == MainAssemblerName);
            m_MainGasGenerator = GridTerminalSystem.GetBlockWithName(MainGasGeneratorName) as IMyGasGenerator;

            m_FrontLcdOres = GridTerminalSystem.GetBlockWithName(OresIngotsLcdName) as IMyTextSurface;
            m_FrontLcdComponents = GridTerminalSystem.GetBlockWithName(ComponentsLcdName) as IMyTextSurface;

            #region Лист заказов кроме стальных пластин
            m_OrderList = new List<MyItemType>();
            m_OrderList.Add(TypeInteriorPlate);
            m_OrderList.Add(TypeConstructionComponent);
            m_OrderList.Add(TypeMetalGrid);
            m_OrderList.Add(TypeComputer);
            m_OrderList.Add(TypeLargeTube);
            m_OrderList.Add(TypeSmallTube);
            m_OrderList.Add(TypeMotor);
            m_OrderList.Add(TypeDisplay);
            m_OrderList.Add(TypePowerCell);
            m_OrderList.Add(TypeGirder);
            m_OrderList.Add(TypeDetector);
            m_OrderList.Add(TypeRadioCommunication);
            m_OrderList.Add(TypeBulletproofGlass);
            #endregion

            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main(string args)
        {
            var mainInventory = m_MainCargoContainer.GetInventory(0);
            var toolInventory = m_ToolsCargoContainer.GetInventory(0);

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

            //Отправка тулзов в контейнер с тулзами, а баллонов в водородник
            bool sorted = false;
            while (!sorted)
            {
                sorted = true;
                for (int i = mainInventory.ItemCount - 1; i >= 0; --i)
                {
                    var item = mainInventory.GetItemAt(i);
                    if (!item.HasValue)
                    {
                        Echo("!item.HasValue exception");
                        break;
                    }
                    if (item.Value.Type.TypeId != TypeIdOres &&
                        item.Value.Type.TypeId != TypeIdIngots &&
                        item.Value.Type.TypeId != TypeIdComponents)
                    {
                        if (item.Value.Type.SubtypeId == SubTypeIdHydrogenBottle ||
                            item.Value.Type.SubtypeId == SubTypeIdOxygenBottle)
                            m_MainGasGenerator.GetInventory(0).TransferItemFrom(mainInventory, item.Value);
                        else
                            toolInventory.TransferItemFrom(mainInventory, item.Value);

                        sorted = false;
                    }
                    else
                    {
                        MyInventoryItem? prevItem;
                        if (i != 0 && (prevItem = mainInventory.GetItemAt(i - 1)).HasValue)
                        {
                            if (prevItem.Value.Type == item.Value.Type)
                            {
                                mainInventory.TransferItemFrom(mainInventory, i, i - 1, true);
                                sorted = false;
                            }
                            else
                            {
                                var itemSortOrder = MyItemTypeComparer.GetTypeSortorder(item.Value.Type);
                                var prevSortOrder = MyItemTypeComparer.GetTypeSortorder(prevItem.Value.Type);
                                if (itemSortOrder < prevSortOrder)
                                {
                                    mainInventory.TransferItemFrom(mainInventory, i, i - 1);
                                    sorted = false;
                                }
                            }
                        }
                    }
                }
            }

            //Подсчёт всех ресурсов на базе
            var resourcesOnFactory = new Dictionary<MyItemType, MyFixedPoint>();
            SummaryItemsOnFactory(resourcesOnFactory);

            //Заказ в ассемблерах
            OrderInAssemblers(resourcesOnFactory);
        }

        private void OrderInAssemblers(Dictionary<MyItemType, MyFixedPoint> resourcesOnFactory)
        {
            var steelPlates = TypeSteelPlate;
            OrderItemType(resourcesOnFactory, steelPlates, SteelPlatesOrderTarget);

            foreach (var item in m_OrderList)
            {
                OrderItemType(resourcesOnFactory, item, OtherComponentsOrderTarget);
            }
            Echo(m_LastOrder);
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
        }

        private string GetBlueprintName(MyItemType itemType)
        {
            switch (itemType.TypeId)
            {
                case TypeIdComponents:
                    switch (itemType.SubtypeId)
                    {
                        case SubTypeIdConstructionComponent:
                        case SubTypeIdComputer:
                        case SubTypeIdDetector:
                        case SubTypeIdMotor:
                        case SubTypeIdGirder:
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
            var otherItems = new List<KeyValuePair<MyItemType, MyFixedPoint>>(); //Currently

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
                        case SubTypeIdIronIngot:
                            name = "Iron Ingots";
                            break;

                        case SubTypeIdNickelIngot:
                            name = "Nickel Ingots";
                            break;

                        case SubTypeIdSiliconIngot:
                            name = "Silicon Ingots";
                            break;

                        case SubTypeIdCobaltIngot:
                            name = "Cobalt Ingots";
                            break;

                        case SubTypeIdSilverIngot:
                            name = "Silver Ingots";
                            break;

                        case SubTypeIdStoneIngot:
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