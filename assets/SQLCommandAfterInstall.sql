-- Description: SQL commands to run after the installation of the database
-- You need those credentials to connect to the database and import tables
CREATE LOGIN "YourName" WITH PASSWORD = 'YourPassword';
ALTER SERVER ROLE sysadmin ADD MEMBER "YourName";
-- Disable the sa account
ALTER LOGIN sa DISABLE;