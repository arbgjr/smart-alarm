-- Smart Alarm Database Initialization Script
-- This script sets up the basic database structure for development

-- Create replication user (for production replica setup)
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = 'replicator') THEN
        CREATE USER replicator WITH REPLICATION ENCRYPTED PASSWORD 'replicator123';
    END IF;
END
$$;

-- Create application schema
CREATE SCHEMA IF NOT EXISTS smartalarm;

-- Create extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";
CREATE EXTENSION IF NOT EXISTS "btree_gin";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";

-- Grant permissions to smartalarm user
GRANT ALL PRIVILEGES ON SCHEMA smartalarm TO smartalarm;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA smartalarm TO smartalarm;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA smartalarm TO smartalarm;
GRANT ALL PRIVILEGES ON ALL FUNCTIONS IN SCHEMA smartalarm TO smartalarm;

-- Set default privileges for future objects
ALTER DEFAULT PRIVILEGES IN SCHEMA smartalarm GRANT ALL ON TABLES TO smartalarm;
ALTER DEFAULT PRIVILEGES IN SCHEMA smartalarm GRANT ALL ON SEQUENCES TO smartalarm;
ALTER DEFAULT PRIVILEGES IN SCHEMA smartalarm GRANT ALL ON FUNCTIONS TO smartalarm;

-- Create a separate database for Hangfire (background jobs)
SELECT 'CREATE DATABASE smartalarm_hangfire OWNER smartalarm'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'smartalarm_hangfire')\gexec

-- Create a separate database for testing
SELECT 'CREATE DATABASE smartalarm_test OWNER smartalarm'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'smartalarm_test')\gexec

-- Create audit schema for audit logs
CREATE SCHEMA IF NOT EXISTS audit;
GRANT ALL PRIVILEGES ON SCHEMA audit TO smartalarm;
ALTER DEFAULT PRIVILEGES IN SCHEMA audit GRANT ALL ON TABLES TO smartalarm;
ALTER DEFAULT PRIVILEGES IN SCHEMA audit GRANT ALL ON SEQUENCES TO smartalarm;

-- Create logging schema for application logs
CREATE SCHEMA IF NOT EXISTS logging;
GRANT ALL PRIVILEGES ON SCHEMA logging TO smartalarm;
ALTER DEFAULT PRIVILEGES IN SCHEMA logging GRANT ALL ON TABLES TO smartalarm;
ALTER DEFAULT PRIVILEGES IN SCHEMA logging GRANT ALL ON SEQUENCES TO smartalarm;

-- Set search path to include all schemas
ALTER USER smartalarm SET search_path = smartalarm, audit, logging, public;

-- Create basic audit function for tracking changes
CREATE OR REPLACE FUNCTION audit.audit_trigger_function()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'DELETE' THEN
        INSERT INTO audit.audit_log (
            table_name, operation, old_values, changed_by, changed_at
        ) VALUES (
            TG_TABLE_NAME, TG_OP, row_to_json(OLD), current_user, now()
        );
        RETURN OLD;
    ELSIF TG_OP = 'UPDATE' THEN
        INSERT INTO audit.audit_log (
            table_name, operation, old_values, new_values, changed_by, changed_at
        ) VALUES (
            TG_TABLE_NAME, TG_OP, row_to_json(OLD), row_to_json(NEW), current_user, now()
        );
        RETURN NEW;
    ELSIF TG_OP = 'INSERT' THEN
        INSERT INTO audit.audit_log (
            table_name, operation, new_values, changed_by, changed_at
        ) VALUES (
            TG_TABLE_NAME, TG_OP, row_to_json(NEW), current_user, now()
        );
        RETURN NEW;
    END IF;
    RETURN NULL;
END;
$$ LANGUAGE plpgsql;

-- Create audit log table
CREATE TABLE IF NOT EXISTS audit.audit_log (
    id BIGSERIAL PRIMARY KEY,
    table_name TEXT NOT NULL,
    operation TEXT NOT NULL,
    old_values JSONB,
    new_values JSONB,
    changed_by TEXT NOT NULL DEFAULT current_user,
    changed_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT now()
);

-- Create index for better performance
CREATE INDEX IF NOT EXISTS idx_audit_log_table_name ON audit.audit_log(table_name);
CREATE INDEX IF NOT EXISTS idx_audit_log_changed_at ON audit.audit_log(changed_at);
CREATE INDEX IF NOT EXISTS idx_audit_log_changed_by ON audit.audit_log(changed_by);

-- Create application log table for structured logging
CREATE TABLE IF NOT EXISTS logging.application_logs (
    id BIGSERIAL PRIMARY KEY,
    timestamp TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT now(),
    level VARCHAR(20) NOT NULL,
    logger VARCHAR(255) NOT NULL,
    message TEXT NOT NULL,
    exception TEXT,
    properties JSONB,
    correlation_id UUID,
    user_id UUID,
    trace_id VARCHAR(32),
    span_id VARCHAR(16)
);

-- Create indexes for application logs
CREATE INDEX IF NOT EXISTS idx_app_logs_timestamp ON logging.application_logs(timestamp);
CREATE INDEX IF NOT EXISTS idx_app_logs_level ON logging.application_logs(level);
CREATE INDEX IF NOT EXISTS idx_app_logs_correlation_id ON logging.application_logs(correlation_id);
CREATE INDEX IF NOT EXISTS idx_app_logs_user_id ON logging.application_logs(user_id);
CREATE INDEX IF NOT EXISTS idx_app_logs_trace_id ON logging.application_logs(trace_id);

-- Create a function to clean old logs (retention policy)
CREATE OR REPLACE FUNCTION logging.cleanup_old_logs(retention_days INTEGER DEFAULT 30)
RETURNS INTEGER AS $$
DECLARE
    deleted_count INTEGER;
BEGIN
    DELETE FROM logging.application_logs
    WHERE timestamp < now() - (retention_days || ' days')::INTERVAL;

    GET DIAGNOSTICS deleted_count = ROW_COUNT;
    RETURN deleted_count;
END;
$$ LANGUAGE plpgsql;

-- Create a function to clean old audit logs (retention policy)
CREATE OR REPLACE FUNCTION audit.cleanup_old_audit_logs(retention_days INTEGER DEFAULT 365)
RETURNS INTEGER AS $$
DECLARE
    deleted_count INTEGER;
BEGIN
    DELETE FROM audit.audit_log
    WHERE changed_at < now() - (retention_days || ' days')::INTERVAL;

    GET DIAGNOSTICS deleted_count = ROW_COUNT;
    RETURN deleted_count;
END;
$$ LANGUAGE plpgsql;
