/* Add Lynicon Roles into standard Identity tables, and give all existing users full Admin rights */

INSERT INTO AspNetRoles (Id, Name)
VALUES ('A', 'A'), ('E', 'E'), ('U', 'U');

INSERT INTO AspNetUserRoles (UserId, RoleId)
SELECT anu.Id, 'A' FROM AspNetUsers anu
UNION
SELECT anu.Id, 'E' FROM AspNetUsers anu
UNION
SELECT anu.Id, 'U' FROM AspNetUsers anu;