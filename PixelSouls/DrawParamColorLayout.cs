// See https://aka.ms/new-console-template for more information
public class DrawParamColorLayout
{
    public string rowName;
    public string colorRCellName;
    public string colorGCellName;
    public string colorBCellName;
    public ColorConvertType convertType;

    public DrawParamColorLayout(string rowName, string colorRCellName, string colorGCellName, string colorBCellName, ColorConvertType convertType)
    {
        this.rowName = rowName;
        this.colorRCellName = colorRCellName;
        this.colorGCellName = colorGCellName;
        this.colorBCellName = colorBCellName;
        this.convertType = convertType;
    }
}