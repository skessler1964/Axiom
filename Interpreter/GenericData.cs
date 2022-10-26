using System;
using System.Text;
using log4net;
using MarketData.DataAccess;
using MarketData.MarketDataModel;

// FileName : GenericData.cs
// Author : Sean Kessler

namespace Axiom.Interpreter
{
  public class GenericData
  {
    private static ILog logger = LogManager.GetLogger(typeof(GenericData));
    private Object objData = null;
    private String objDataType = "";
    private bool isModified=false;

    public Object Data
    {
      set
      {
        objData = value;
        if(null!=objData)objDataType =objData.GetType().ToString();
      }
      get
      {
        return objData;
      }
    }
    public bool IsNull()
    {
      return null == objData ? true : false;
    }
    public bool IsModified
    {
      get {return isModified;}
      set {isModified=value;}
    }
    public static GenericData PerformConvert(GenericData data1, GenericData data2)    // convert data of param1 to the type described in data2
    {
      GenericData genericData = new GenericData();
      genericData.SetData(data1.GetStringRepData(),data2.Get<String>());
      return genericData;
    }
		public GenericData GetPrice(GenericData dateData,GenericData opData)
		{
      GenericData resultData = new GenericData();
			double result=default(double);
      try
      {
			  String strSymbol=this.Get<String>();
        DateTime date = dateData.Get<DateTime>();
				Price price=PricingDA.GetPrice(strSymbol,date);
				if(null != price)
				{
				  String strOperation=opData.Get<String>().ToUpper();
					if(strOperation.Equals("OPEN"))result = price.Open;
					else if(strOperation.Equals("HIGH"))result = price.High;
					else if(strOperation.Equals("LOW"))result = price.Low;
					else result = price.Close;
			  }
      }
      catch (Exception)
      {
      }
      resultData.SetData(result);
      return resultData;
		}
    public GenericData Substring(GenericData startIndexData,GenericData lengthData)   
    {
      GenericData resultData = new GenericData();
      String result = null;
      try
      {
        int startIndex = startIndexData.Get<int>();
        int length = lengthData.Get<int>();
        String strArgument = this.Get<String>();
        result = strArgument.Substring(startIndex - 1, length);
      }
      catch (Exception)
      {
      }
      resultData.SetData(result);
      return resultData;
    }
    public GenericData Trim()   
    {
      GenericData resultData = new GenericData();
      String result = null;
      try
      {
        String strArgument = this.Get<String>();
        result = strArgument.Trim();
      }
      catch (Exception)
      {
      }
      resultData.SetData(result);
      return resultData;
    }
    public GenericData Upper()   
    {
      GenericData resultData = new GenericData();
      String result = null;
      try
      {
        String strArgument = this.Get<String>();
        result = strArgument.ToUpper();
      }
      catch (Exception)
      {
      }
      resultData.SetData(result);
      return resultData;
    }
    public GenericData Lower()   
    {
      GenericData resultData = new GenericData();
      String result = null;
      try
      {
        String strArgument = this.Get<String>();
        result = strArgument.ToLower();
      }
      catch (Exception)
      {
      }
      resultData.SetData(result);
      return resultData;
    }
    public GenericData Like(GenericData argument) 
    {
      GenericData genericData = new GenericData();
      String thisString = this.Get<String>();
      String strArgument = argument.GetStringRepData();
      if (null == thisString || null == strArgument)
      {
        genericData.SetData(false);
        return genericData;
      }
      strArgument = strArgument.Replace("%", null);
      genericData.SetData(thisString.Contains(strArgument));
      return genericData;
    }
    public void SetNull()
    {
      objDataType="System.Nullable";
      objData=Convert.ChangeType(null,Type.GetType(objDataType));
    }
    public void SetNull(String strDataType)
    {
      objDataType=GetTypeString(strDataType);
      objData=Convert.ChangeType(null,Type.GetType(objDataType));
    }
    public void SetData(double data)
    {
      Type targetType = data.GetType();
      objDataType = targetType.Name;
      objData = data;
    }
    public void SetData(bool data)
    {
      Type targetType = data.GetType();
      objDataType = targetType.Name;
      objData = data;
    }
    public void SetData(String data)
    {
      Type targetType = data.GetType();
      objDataType = targetType.Name;
      objData = data;
    }
    public void SetData(String stringRep, String typeName)
    {
      Type targetType = Type.GetType(typeName);
      objDataType = typeName;
      objData = Convert.ChangeType(stringRep, targetType);
    }
    public static GenericData Clone(GenericData genericData)
    {
      GenericData copyGenericData = new GenericData();
      if (null == genericData) return copyGenericData;
      copyGenericData.SetData(genericData.GetStringRepData(),genericData.GetStringRepType());
      return copyGenericData;
    }
// *************************************************************************************************************************************************************************************
// *********************************************************************************** L O G I C A L   O P E R A T I O N S *************************************************************
// *************************************************************************************************************************************************************************************
    public GenericData Not()
    {
      GenericData data = new GenericData();
      String stringRepType = GetStringRepType();

      if (IsNull())
      {
        data.SetNull();
      }
      else if ("System.Boolean".Equals(stringRepType))
      {
        data.Data=!(Get<Boolean>());
      }
      else if ("System.Int32".Equals(stringRepType))
      {
        data.Data = (Get<Int32>() == 0);
      }
      else if ("System.String".Equals(stringRepType))
      {
        data.Data = !(Get<Boolean>());
      }
      else if ("System.Double".Equals(stringRepType))
      {
        data.Data = (Get<double>() == 0.00);
      }
      else data.SetNull();
      return data;
    }
    public GenericData LessEqual(GenericData genericData)
    {
      GenericData data = new GenericData();
      if (IsNull())
      {
        data.SetData(false);
      }
      else if ("System.Double".Equals(genericData.GetStringRepType()))
      {
        try
        { 
          data.Data = Get<double>()<=genericData.Get<double>(); 
        }
        catch (Exception) { data.Data = 0.00; }
      }
      else if ("System.Int32".Equals(genericData.GetStringRepType()))
      {
        try
        { 
          data.Data = Get<Int32>()<=genericData.Get<Int32>(); 
        }
        catch (Exception) { data.Data = 0.00; }
      }
      else if ("System.String".Equals(genericData.GetStringRepType()))
      {
        data.Data = Get<String>().Length<=genericData.Get<String>().Length;
      }
      else data.SetNull();
      return data;
    }
    public GenericData Or(GenericData genericData)
    {
      GenericData data = new GenericData();
      if (IsNull())
      {
        data.SetData(false);
      }
      else if ("System.Double".Equals(genericData.GetStringRepType()))
      {
        try
        { 
          data.Data = (0.00!=Get<double>())||(0.00!=genericData.Get<double>()); 
        }
        catch (Exception) { data.Data = 0.00; }
      }
      else if ("System.Int32".Equals(genericData.GetStringRepType()))
      {
        try
        { 
          data.Data = (0!=Get<Int32>())||(0!=genericData.Get<Int32>()); 
        }
        catch (Exception) { data.Data = 0.00; }
      }
      else if ("System.Boolean".Equals(genericData.GetStringRepType()))
      {
        data.Data = Get<Boolean>()||genericData.Get<Boolean>();
      }
      else data.SetNull();
      return data;
    }
    public GenericData And(GenericData genericData)
    {
      GenericData data = new GenericData();
      if (IsNull())
      {
        data.SetData(false);
      }
      else if ("System.Double".Equals(genericData.GetStringRepType()))
      {
        try
        {
          data.Data = (0.00 != Get<double>()) && (0.00 != genericData.Get<double>());
        }
        catch (Exception) { data.Data = 0.00; }
      }
      else if ("System.Int32".Equals(genericData.GetStringRepType()))
      {
        try
        {
          data.Data = (0 != Get<Int32>()) && (0 != genericData.Get<Int32>());
        }
        catch (Exception) { data.Data = 0.00; }
      }
      else if ("System.Boolean".Equals(genericData.GetStringRepType()))
      {
        data.Data = Get<Boolean>() && genericData.Get<Boolean>();
      }
      else data.SetNull();
      return data;
    }
    public GenericData Greater(GenericData genericData)
    {
      GenericData data = new GenericData();
      if (IsNull())
      {
        data.SetData(false);
      }
      else if ("System.Double".Equals(genericData.GetStringRepType()))
      {
        try
        { 
          data.Data = Get<double>()>genericData.Get<double>(); 
        }
        catch (Exception) { data.Data = 0.00; }
      }
      else if ("System.Int32".Equals(genericData.GetStringRepType()))
      {
        try
        { 
          data.Data = Get<Int32>()>genericData.Get<Int32>(); 
        }
        catch (Exception) { data.Data = 0.00; }
      }
      else if ("System.String".Equals(genericData.GetStringRepType()))
      {
        data.Data = Get<String>().Length>genericData.Get<String>().Length;
      }
      else data.SetNull();
      return data;
    }
    public GenericData GreaterEqual(GenericData genericData)
    {
      GenericData data = new GenericData();
      if (IsNull())
      {
        data.SetData(false);
      }
      else if ("System.Double".Equals(genericData.GetStringRepType()))
      {
        try
        { 
          data.Data = Get<double>()>=genericData.Get<double>(); 
        }
        catch (Exception) { data.Data = 0.00; }
      }
      else if ("System.Int32".Equals(genericData.GetStringRepType()))
      {
        try
        { 
          data.Data = Get<Int32>()>=genericData.Get<Int32>(); 
        }
        catch (Exception) { data.Data = 0.00; }
      }
      else if ("System.String".Equals(genericData.GetStringRepType()))
      {
        data.Data = Get<String>().Length>=genericData.Get<String>().Length;
      }
      else data.SetNull();
      return data;
    }
    public GenericData EqualEqual(GenericData genericData)
    {
      GenericData data = new GenericData();

      if (IsNull() && genericData.IsNull())
      {
        data.SetData(true);
      }
      else if (IsNull())
      {
        data.SetData(false);
      }
      else if ("System.Double".Equals(genericData.GetStringRepType()))
      {
        try { data.Data = (0 == Get<double>().CompareTo(genericData.Get<double>())); }
        catch (Exception) { data.Data = 0.00; }
      }
      else if ("System.Int32".Equals(genericData.GetStringRepType()))
      {
        try { data.Data = (0 == Get<Int32>().CompareTo(genericData.Get<Int32>())); }
        catch (Exception) { data.Data = 0.00; }
      }
      else if ("System.String".Equals(genericData.GetStringRepType()))
      {
        data.Data = (Get<String>().Equals(genericData.Get<String>()));
      }
      else data.SetNull();
      return data;
    }
    public GenericData NotEqual(GenericData genericData)
    {
      GenericData data = new GenericData();
      if (IsNull() && !genericData.IsNull())
      {
        data.SetData(true);
      }
      else if (!IsNull() && genericData.IsNull())
      {
        data.SetData(true);
      }
      else if (IsNull())
      {
        data.SetData(false);
      }
      else if ("System.Double".Equals(genericData.GetStringRepType()))
      {
        try { data.Data = (0 != Get<double>().CompareTo(genericData.Get<double>())); }
        catch (Exception) { data.Data = 0.00; }
      }
      else if ("System.Int32".Equals(genericData.GetStringRepType()))
      {
        try { data.Data = (0 != Get<Int32>().CompareTo(genericData.Get<Int32>())); }
        catch (Exception) { data.Data = 0.00; }
      }
      else if ("System.String".Equals(genericData.GetStringRepType()))
      {
        data.Data = !(Get<String>().Equals(genericData.Get<String>()));
      }
      else data.SetNull();
      return data;
    }
    public GenericData Less(GenericData genericData)
    {
      GenericData data = new GenericData();
      if (IsNull())
      {
        data.SetData(false);
      }
      else if ("System.Double".Equals(genericData.GetStringRepType()))
      {
        try { data.Data = (-1==Get<double>().CompareTo(genericData.Get<double>())); }
        catch (Exception) { data.Data = 0.00; }
      }
      else if ("System.Int32".Equals(genericData.GetStringRepType()))
      {
        try { data.Data = (-1==Get<Int32>().CompareTo(genericData.Get<Int32>())); }
        catch (Exception) { data.Data = 0.00; }
      }
      else if ("System.String".Equals(genericData.GetStringRepType()))
      {
        data.Data = Get<String>().Equals(genericData.Get<String>());
      }
      else data.SetNull();
      return data;
    }
    // **************************************************************************************************************************************************************************************
    // ********************************************************************************** M A T H E M A T I C A L   O P E R A T I O N S  ****************************************************
    // **************************************************************************************************************************************************************************************
    public GenericData Negate()
    {
      GenericData data = new GenericData();
      String stringRepType = GetStringRepType();

      if (IsNull())
      {
        data.SetNull();
      }
      else if ("System.Double".Equals(stringRepType))
      {
        try
        {
          data.Data = Get<double>() * -1.00;
        }
        catch (Exception) { data.Data = 0.00; }
      }
      else
      {
        try
        {
          data.Data = Get<Int32>() * -1;
        }
        catch (Exception){data.SetNull();}
      }
      return data;
    }
    public GenericData Abs()
    {
      GenericData data = new GenericData();
      String stringRepType = GetStringRepType();
      if (IsNull())
      {
        data.SetNull();
      }
      if ("System.Double".Equals(stringRepType))
      {
        try
        {
          data.Data = Math.Abs(Get<double>());
        }
        catch (Exception) { data.Data = 0.00; }
      }
      else
      {
        try
        {
          data.Data = Math.Abs(Get<Int32>());
        }
        catch (Exception)
        {
          data.SetNull();
        }
      }
      return data;
    }

    public GenericData Multiply(GenericData genericData)
    {
      GenericData data = new GenericData();
      if (IsNull())
      {
        data.SetNull();
      }
      else if ("System.Double".Equals(genericData.GetStringRepType()))
      {
        try { data.Data = Get<double>() * genericData.Get<double>(); }
        catch (Exception) { data.Data = double.NaN; }
      }
      else if ("System.Int32".Equals(genericData.GetStringRepType()))
      {
        try { data.Data = Get<Int32>() * genericData.Get<Int32>(); }
        catch (Exception) { data.Data = Int32.MinValue; }
      }
      else if ("System.String".Equals(genericData.GetStringRepType()))
      {
        data.Data = double.NaN;
      }
      else data.SetNull();
      return data;
    }
    public GenericData Divide(GenericData genericData)
    {
      GenericData data = new GenericData();
      if (IsNull())
      {
        data.SetNull();
      }
      else if ("System.Double".Equals(genericData.GetStringRepType()))
      {
        try { data.Data = Get<double>() / genericData.Get<double>(); }
        catch (Exception) { data.Data = double.NaN; }
      }
      else if ("System.Int32".Equals(genericData.GetStringRepType()))
      {
        try { data.Data = Get<Int32>() / genericData.Get<Int32>(); }
        catch (Exception) { data.Data = Int32.MinValue; }
      }
      else if ("System.String".Equals(genericData.GetStringRepType()))
      {
        data.Data = double.NaN;
      }
      else data.SetNull();
      return data;
    }
    public GenericData Add(GenericData genericData)
    {
      GenericData data = new GenericData();
      if (IsNull())
      {
        data.SetNull();
      }
      else if ("System.Double".Equals(genericData.GetStringRepType()))
      {
        data.Data = Get<double>()+genericData.Get<double>();
      }
      else if ("System.Int32".Equals(genericData.GetStringRepType()))
      {
        data.Data = Get<Int32>()+genericData.Get<Int32>();
      }
      else if ("System.String".Equals(genericData.GetStringRepType()))
      {
        data.Data = Get<String>() + genericData.Get<String>();
      }
      else data.SetNull();
      return data;
    }
    public GenericData Subtract(GenericData genericData)
    {
      GenericData data = new GenericData();
      if (IsNull())
      {
        data.SetNull();
      }
      else if ("System.Double".Equals(genericData.GetStringRepType()))
      {
        data.Data = Get<double>() - genericData.Get<double>();
      }
      else if ("System.Int32".Equals(genericData.GetStringRepType()))
      {
        data.Data = Get<Int32>() - genericData.Get<Int32>();
      }
      else if ("System.String".Equals(genericData.GetStringRepType()))
      {
        data.Data = (Get<String>()).Replace(genericData.Get<String>(), null);
      }
      else data.SetNull();
      return data;
    }
// ********************************************************************************************************** E N D   O P E R A T I O N S  ********************************************************************************************
    public String GetStringRepData()
    {
      if (null == objData) return null;
      String stringRepType = GetStringRepType();
      return (String)Convert.ChangeType(objData, typeof(String));
    }
    public String GetStringRepType()
    {
      if (null == objData) return "System.Nullable";
      return objData.GetType().ToString();
    }
    public Type DataType
    {
      get
      {
        if (null == objData) throw new Exception("No type data for null value.");
        return objData.GetType();
      }
    }
    public override String ToString()
    {
      StringBuilder sb = new StringBuilder();
      sb.Append(GetStringRepType()).Append(":").Append("'").Append(GetStringRepData()).Append("'");
      return sb.ToString();
    }
    public T Get<T>()
    {
      T result = default(T);
      try
      {
        result = (T)Convert.ChangeType(objData, typeof(T));
      }
      catch(Exception /*exception*/)
      {
        result = default(T);
      }
      return result;
    }
// **********************************************************************************************************************************************************************************
// ************************************************************************ P U B L I C  S T A T I C  H E L P E R S  ****************************************************************
// **********************************************************************************************************************************************************************************
    public static Object ConvertType(String destinationType,String stringRep)
    {
      try
      {
        if (destinationType.Equals("System.DBNull")) destinationType = "System.String";
        Type targetType = Type.GetType(destinationType);
        String objDataType = destinationType;
        Object objData = Convert.ChangeType(stringRep, targetType);
        return objData;
      }
      catch(Exception /*exception*/)
      {
        return null;
      }
    }
    public static Object ConvertType(Type targetType,String stringRep)
    {
      try
      {
        Object objData = null;
        String stringRepType = targetType.ToString();
        if(targetType.Equals("System.Double")&&null==stringRep||stringRep.Equals(""))return objData;
        else if (stringRepType.Equals("System.DBNull")) stringRep = "System.String";
        objData = Convert.ChangeType(stringRep, targetType);
        return objData;
      }
      catch(Exception /*exception*/)
      {
        return null;
      }
    }
    public static Type GetType(String strType)
    {
      strType=strType.ToUpper();
      if(strType.Equals("STRING")||strType.Equals("SYSTEM.STRING"))return Type.GetType("System.String");
      else if(strType.Equals("NUMERIC")||strType.Equals("DOUBLE")||strType.Equals("SYSTEM.DOUBLE"))return Type.GetType("System.Double");
      else if(strType.Equals("DATETIME")||strType.Equals("SYSTEM.DATETIME"))return Type.GetType("System.DateTime");
      return Type.GetType("System.String");
    }
    public static String GetTypeString(String strType)
    {
      strType=strType.ToUpper();
      if(strType.Equals("STRING")||strType.Equals("SYSTEM.STRING"))return "System.String";
      else if(strType.Equals("NUMERIC")||strType.Equals("DOUBLE")||strType.Equals("SYSTEM.DOUBLE"))return "System.Double";
      else if(strType.Equals("DATETIME")||strType.Equals("SYSTEM.DATETIME"))return "System.DateTime";
      return "System.String";
    }
    public static String ToExpressionData(System.Data.DataColumn dataColumn,String value)
    {
      if (dataColumn.DataType.Name.ToUpper().Equals("STRING")) return "'"+Utility.Utility.SqlString(value)+"'";
      if (dataColumn.DataType.Name.ToUpper().Equals("DATETIME")) return "'"+Utility.Utility.SqlString(value)+"'";
      return value;
    }
  }
}
