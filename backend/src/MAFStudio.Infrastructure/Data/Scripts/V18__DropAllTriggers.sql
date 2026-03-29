-- V18__DropAllTriggers.sql
-- 删除所有触发器

-- 获取所有触发器并删除
DO $$
DECLARE
    trig RECORD;
BEGIN
    FOR trig IN 
        SELECT trigger_name, event_object_table, event_object_schema
        FROM information_schema.triggers
        WHERE trigger_schema NOT IN ('pg_catalog', 'information_schema')
    LOOP
        EXECUTE format('DROP TRIGGER IF EXISTS %I ON %I.%I CASCADE', 
            trig.trigger_name, 
            trig.event_object_schema, 
            trig.event_object_table);
        RAISE NOTICE '已删除触发器: % on %.%', trig.trigger_name, trig.event_object_schema, trig.event_object_table;
    END LOOP;
END $$;

-- 删除所有触发器函数
DO $$
DECLARE
    func RECORD;
BEGIN
    FOR func IN 
        SELECT p.proname, n.nspname
        FROM pg_proc p
        JOIN pg_namespace n ON p.pronamespace = n.oid
        WHERE n.nspname NOT IN ('pg_catalog', 'information_schema')
        AND p.prokind = 'f'
        AND p.proname LIKE '%trigger%'
    LOOP
        EXECUTE format('DROP FUNCTION IF EXISTS %I.%I CASCADE', func.nspname, func.proname);
        RAISE NOTICE '已删除函数: %.%', func.nspname, func.proname;
    END LOOP;
END $$;
