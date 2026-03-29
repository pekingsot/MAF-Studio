-- 为 admin 用户分配超级管理员角色
-- admin 用户 ID: 00000000-0000-0000-0000-000000000001
-- 超级管理员角色 ID: 1000000000000001

-- 首先删除 admin 用户的所有角色
DELETE FROM user_roles WHERE user_id = '00000000-0000-0000-0000-000000000001';

-- 为 admin 用户分配超级管理员角色
INSERT INTO user_roles (id, user_id, role_id, created_at)
VALUES (5000000000000001, '00000000-0000-0000-0000-000000000001', 1000000000000001, CURRENT_TIMESTAMP)
ON CONFLICT (user_id, role_id) DO NOTHING;

-- 为 pekingsot 用户分配普通用户角色（如果还没有）
INSERT INTO user_roles (id, user_id, role_id, created_at)
SELECT 5000000000000002, id, 1000000000000003, CURRENT_TIMESTAMP
FROM users
WHERE username = 'pekingsot'
AND id NOT IN (SELECT user_id FROM user_roles WHERE role_id = 1000000000000003)
ON CONFLICT DO NOTHING;
