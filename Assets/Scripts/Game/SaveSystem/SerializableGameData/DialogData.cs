using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;

/// <summary>
/// Данные о всех вызванных диалогах
/// </summary>
[XmlType("Dialog Data")]
[XmlInclude(typeof(DialogInfo))]
public class DialogData
{
    [XmlArray("Dialogs Info")]
    [XmlArrayItem("Dialog Info")]
    public List<DialogInfo> dialogs = new List<DialogInfo>();

    public DialogData()
    { }

    public DialogData(List<DialogQueueElement> dialogQueue)
    {
        dialogs = new List<DialogInfo>();
        foreach (DialogQueueElement dElement in dialogQueue)
            dialogs.Add(new DialogInfo(dElement.npc != null ? dElement.npc.GetID() : -1, dElement.dialog.dialogName));
    }

}

/// <summary>
/// Информация об одном вызванном диалоге
/// </summary>
[XmlType("DialogInfo")]
public class DialogInfo
{
    [XmlElement("NPC ID")]
    public int npcID;//ID NPC, который вызывает диалог

    [XmlElement("Dialog Name")]
    public string dialogName;//Название этого диалога

    public DialogInfo()
    { }

    public DialogInfo(int _id, string _dialogName)
    {
        npcID = _id;
        dialogName = _dialogName;
    }

}