-- Create replication user
CREATE USER replicator WITH REPLICATION ENCRYPTED PASSWORD 'replicator123';

-- Create application schema
CREATE SCHEMA IF NOT EXISTS smartalarm;

-- Grant permissions
GRANT ALL PRIVILEGES ON SCHEMA smartalarm TO smartalarm;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA smartalarm TO smartalarm;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA smartalarm TO smartalarm;

-- Create extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";
