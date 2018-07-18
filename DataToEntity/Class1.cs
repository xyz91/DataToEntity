using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Sorvy
{
    public class DataToEntity
    {
        private delegate List<T> mapT<T>(DbDataReader dr);
        private static Dictionary<Type, Delegate> cachedMappers = new Dictionary<Type, Delegate>();
        private static Dictionary<Type, Delegate> cachedMappers1 = new Dictionary<Type, Delegate>();
        /// <summary>
        /// 非严格模式转换,默认为实体类的所有属性或者字段都在DbDataReader中有对应的数据列
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dr"></param>
        /// <returns></returns>
        public static List<T> MapToEntities<T>(DbDataReader dr) {
          return   MapToEntities<T>(dr, false);
        }
        /// <summary>
        /// 严格模式会检查数据行中是否含有此属性及数据是否为Null,比快速模式慢一点
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dr"></param>
        /// <param name="isStrict">是否严格模式</param>
        /// <returns></returns>
        public static List<T> MapToEntities<T>(DbDataReader dr, bool isStrict)
        {
            // If a mapping function from dr -> T does not exist, create and cache one
            if (!(isStrict ? cachedMappers : cachedMappers1).ContainsKey(typeof(T)))
            {
                Type[] methodArgs = { typeof(DbDataReader) };
                DynamicMethod dm = new DynamicMethod("MapDR99999", typeof(List<T>), methodArgs, typeof(DataToEntity));
                ILGenerator il = dm.GetILGenerator();
                il.DeclareLocal(typeof(List<T>));
                il.Emit(OpCodes.Newobj, typeof(List<T>).GetConstructor(Type.EmptyTypes));
                il.Emit(OpCodes.Stloc_0);

                il.DeclareLocal(typeof(Dictionary<string, int>));
                il.Emit(OpCodes.Newobj, typeof(Dictionary<string, int>).GetConstructor(Type.EmptyTypes));
                il.Emit(OpCodes.Stloc_1);

                il.DeclareLocal(typeof(Boolean));
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Stloc_2);

                il.DeclareLocal(typeof(Int32));
                il.Emit(OpCodes.Ldc_I4_M1);
                il.Emit(OpCodes.Stloc_3);

                il.DeclareLocal(typeof(Boolean));
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Stloc_S, 4);

                var whilestart = il.DefineLabel();
                var readstart = il.DefineLabel();

                il.Emit(OpCodes.Br, readstart);

                il.MarkLabel(whilestart);


                #region while内容

                il.DeclareLocal(typeof(T));
                il.Emit(OpCodes.Newobj, typeof(T).GetConstructor(Type.EmptyTypes));
                il.Emit(OpCodes.Stloc_S, 5);

                foreach (var pi in typeof(T).GetMembers().Where(a => a.MemberType == MemberTypes.Property || a.MemberType == MemberTypes.Field))
                {
                    PropertyInfo pro = null; FieldInfo file = null;
                    bool ispro = true; MethodInfo setter = null;
                    if (pi.MemberType == MemberTypes.Property)
                    {
                        pro = pi as PropertyInfo;
                        setter = pro.SetMethod;
                        if (setter == null)
                        {
                            continue;
                        }
                    }
                    else if (pi.MemberType == MemberTypes.Field)
                    {
                        file = pi as FieldInfo;
                        ispro = false;
                    }
                    Type type = ispro ? pro.PropertyType : file.FieldType;
                    if ((type.IsConstructedGenericType || type.IsArray) && !type.IsValueType)
                    {
                        continue;
                    }
                    var noname = il.DefineLabel();

                    var loc2true = il.DefineLabel();
                    var tempre = il.DefineLabel();
                    var inttemp = il.DefineLabel();
                    var intre = il.DefineLabel();
                    var tempaddkey = il.DefineLabel();


                    il.Emit(OpCodes.Ldloc_2);
                    il.Emit(OpCodes.Brfalse, loc2true);

                    il.BeginExceptionBlock();
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldstr, pi.Name);
                    il.Emit(OpCodes.Callvirt, typeof(DbDataReader).GetMethod("GetOrdinal", new Type[] { typeof(string) }));
                    il.Emit(OpCodes.Stloc, 3);
                    il.Emit(OpCodes.Ldc_I4_1);
                    il.Emit(OpCodes.Stloc_S, 4);
                    il.BeginCatchBlock(typeof(Exception));
                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(OpCodes.Stloc_S, 4);
                    il.EndExceptionBlock();
                    il.Emit(OpCodes.Br, tempre);

                    il.MarkLabel(loc2true);
                    if (isStrict)
                    {
                        #region 判断是否存在2

                        il.Emit(OpCodes.Ldloc_1);
                        il.Emit(OpCodes.Ldstr, pi.Name);
                        il.Emit(OpCodes.Callvirt, typeof(Dictionary<string, int>).GetMethod("ContainsKey", new Type[] { typeof(string) }));
                        il.Emit(OpCodes.Brfalse, tempre);
                        il.Emit(OpCodes.Ldc_I4_1);
                        il.Emit(OpCodes.Stloc_S, 4);

                        #endregion
                    }
                    il.Emit(OpCodes.Ldloc_1);
                    il.Emit(OpCodes.Ldstr, pi.Name);
                    il.Emit(OpCodes.Callvirt, typeof(Dictionary<string, int>).GetMethod("get_Item", new Type[] { typeof(string) }));
                    il.Emit(OpCodes.Stloc_S, 3);

                    il.MarkLabel(tempre);
                    if (isStrict)
                    {
                        #region 是否存在结果
                        il.Emit(OpCodes.Ldloc_S, 4);
                        il.Emit(OpCodes.Brfalse, noname);

                        #endregion

                        #region 判断数据是否为空null
                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Ldloc_3);
                        il.Emit(OpCodes.Callvirt, typeof(DbDataReader).GetMethod("IsDBNull", new Type[] { typeof(int) }));
                        il.Emit(OpCodes.Brtrue, noname);
                        #endregion

                    }
                    il.Emit(OpCodes.Ldloc_2);
                    il.Emit(OpCodes.Brfalse, tempaddkey);
                    il.Emit(OpCodes.Ldloc_1);
                    il.Emit(OpCodes.Ldstr, pi.Name);
                    il.Emit(OpCodes.Ldloc, 3);
                    il.Emit(OpCodes.Callvirt, typeof(Dictionary<string, int>).GetMethod("Add", new Type[] { typeof(string), typeof(int) }));
                    il.MarkLabel(tempaddkey);

                    bool hasvalue = true;

                    il.Emit(OpCodes.Ldloc, 5);
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldloc_3);
                    string proname = type.Name == "Nullable`1" ? type.GenericTypeArguments[0].Name : type.Name;
                    switch (proname)
                    {
                        case "Int16":
                            il.Emit(OpCodes.Callvirt, typeof(DbDataReader).GetMethod("GetInt16", new Type[] { typeof(Int32) })); break;
                        case "Int32":
                            il.Emit(OpCodes.Callvirt, typeof(DbDataReader).GetMethod("GetInt32", new Type[] { typeof(Int32) })); break;
                        case "Int64":
                            il.Emit(OpCodes.Callvirt, typeof(DbDataReader).GetMethod("GetInt64", new Type[] { typeof(Int32) })); break;
                        case "Double":
                            il.Emit(OpCodes.Callvirt, typeof(DbDataReader).GetMethod("GetDouble", new Type[] { typeof(Int32) })); break;
                        case "Single":
                            il.Emit(OpCodes.Callvirt, typeof(DbDataReader).GetMethod("GetFloat", new Type[] { typeof(Int32) })); break;
                        case "Boolean":
                            il.Emit(OpCodes.Callvirt, typeof(DbDataReader).GetMethod("GetBoolean", new Type[] { typeof(Int32) })); break;
                        case "String":
                            il.Emit(OpCodes.Callvirt, typeof(DbDataReader).GetMethod("GetString", new Type[] { typeof(Int32) })); break;
                        case "DateTime":
                            il.Emit(OpCodes.Callvirt, typeof(DbDataReader).GetMethod("GetDateTime", new Type[] { typeof(Int32) })); break;
                        case "Decimal":
                            il.Emit(OpCodes.Callvirt, typeof(DbDataReader).GetMethod("GetDecimal", new Type[] { typeof(Int32) })); break;
                        case "Byte":
                            il.Emit(OpCodes.Callvirt, typeof(DbDataReader).GetMethod("GetByte", new Type[] { typeof(Int32) })); break;
                        case "Char":
                            il.Emit(OpCodes.Callvirt, typeof(DbDataReader).GetMethod("GetChar", new Type[] { typeof(Int32) })); break;
                        case "Guid":
                            il.Emit(OpCodes.Callvirt, typeof(DbDataReader).GetMethod("GetGuid", new Type[] { typeof(Int32) })); break;
                        case "Object":
                            il.Emit(OpCodes.Callvirt, typeof(DbDataReader).GetMethod("GetValue", new Type[] { typeof(Int32) })); break;
                        default:
                            if (type.IsEnum)
                            {
                                il.Emit(OpCodes.Callvirt, typeof(DbDataReader).GetMethod("GetInt32", new Type[] { typeof(Int32) }));
                            }
                            else
                            {
                                il.Emit(OpCodes.Pop);
                                il.Emit(OpCodes.Pop);
                                il.Emit(OpCodes.Pop);
                                hasvalue = false;
                            }
                            break;
                    }

                    if (hasvalue)
                    {
                        if (type.IsValueType && type.IsConstructedGenericType)
                        {
                            il.Emit(OpCodes.Newobj, type.GetConstructor(new Type[] { type.GenericTypeArguments[0] }));
                        }
                        if (ispro)
                        {
                            if (setter != null)
                            {
                                il.Emit(OpCodes.Callvirt, typeof(T).GetMethod("set_" + pro.Name, new Type[] { type }));
                            }
                            else
                            {
                                il.Emit(OpCodes.Pop);
                                il.Emit(OpCodes.Pop);
                            }
                        }
                        else
                        {
                            il.Emit(OpCodes.Stfld, file);
                        }
                    }



                    il.MarkLabel(noname);
                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(OpCodes.Stloc_S, 4);
                }

                il.Emit(OpCodes.Ldloc_0);
                il.Emit(OpCodes.Ldloc_S, 5);
                il.Emit(OpCodes.Callvirt, typeof(List<T>).GetMethod("Add", new Type[] { typeof(T) }));
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Stloc_2);
                #endregion

                il.MarkLabel(readstart);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Callvirt, typeof(DbDataReader).GetMethod("Read", new Type[] { }));
                il.Emit(OpCodes.Brtrue, whilestart);

                il.Emit(OpCodes.Ldloc_0);
                il.Emit(OpCodes.Ret);
                (isStrict ? cachedMappers : cachedMappers1).Add(typeof(T), dm.CreateDelegate(typeof(mapT<T>)));
            }
            mapT<T> invokeMapEntity = (mapT<T>)(isStrict ? cachedMappers : cachedMappers1)[typeof(T)];
            return invokeMapEntity(dr);
        }
    }
}
