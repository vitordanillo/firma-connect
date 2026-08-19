CREATE EXTENSION IF NOT EXISTS citext;
CREATE EXTENSION IF NOT EXISTS pg_trgm;

CREATE TYPE connection_status AS ENUM ('pending', 'accepted', 'declined', 'cancelled');

CREATE TABLE communities (
    id uuid PRIMARY KEY,
    name varchar(120) NOT NULL,
    slug varchar(80) NOT NULL UNIQUE,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE users (
    id uuid PRIMARY KEY,
    email citext NOT NULL UNIQUE,
    display_name varchar(100) NOT NULL,
    password_hash varchar(500) NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz NULL
);

CREATE TABLE community_invitations (
    id uuid PRIMARY KEY,
    community_id uuid NOT NULL REFERENCES communities(id) ON DELETE CASCADE,
    email citext NOT NULL,
    token_hash varchar(64) NOT NULL UNIQUE,
    created_by_user_id uuid NOT NULL REFERENCES users(id),
    expires_at timestamptz NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    used_at timestamptz NULL
);

CREATE TABLE institutions (
    id uuid PRIMARY KEY,
    name varchar(180) NOT NULL,
    normalized_name varchar(180) NOT NULL UNIQUE,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE communities_memberships (
    id uuid PRIMARY KEY,
    community_id uuid NOT NULL REFERENCES communities(id) ON DELETE CASCADE,
    user_id uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    role varchar(20) NOT NULL DEFAULT 'member' CHECK (role IN ('member', 'admin')),
    joined_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (community_id, user_id)
);

CREATE TABLE profiles (
    id uuid PRIMARY KEY,
    community_id uuid NOT NULL REFERENCES communities(id) ON DELETE CASCADE,
    user_id uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    institution_id uuid NULL REFERENCES institutions(id),
    course varchar(120) NULL,
    headline varchar(140) NULL,
    bio varchar(800) NULL,
    contact_url varchar(300) NULL,
    available_for_team boolean NOT NULL DEFAULT false,
    visible_in_directory boolean NOT NULL DEFAULT true,
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (community_id, user_id)
);

CREATE TABLE skills (
    id uuid PRIMARY KEY,
    name varchar(60) NOT NULL,
    normalized_name varchar(60) NOT NULL UNIQUE
);

CREATE TABLE interests (
    id uuid PRIMARY KEY,
    name varchar(60) NOT NULL,
    normalized_name varchar(60) NOT NULL UNIQUE
);

CREATE TABLE profile_skills (
    profile_id uuid NOT NULL REFERENCES profiles(id) ON DELETE CASCADE,
    skill_id uuid NOT NULL REFERENCES skills(id),
    PRIMARY KEY (profile_id, skill_id)
);

CREATE TABLE profile_interests (
    profile_id uuid NOT NULL REFERENCES profiles(id) ON DELETE CASCADE,
    interest_id uuid NOT NULL REFERENCES interests(id),
    PRIMARY KEY (profile_id, interest_id)
);

CREATE TABLE connection_requests (
    id uuid PRIMARY KEY,
    community_id uuid NOT NULL REFERENCES communities(id) ON DELETE CASCADE,
    requester_profile_id uuid NOT NULL REFERENCES profiles(id) ON DELETE CASCADE,
    recipient_profile_id uuid NOT NULL REFERENCES profiles(id) ON DELETE CASCADE,
    note varchar(300) NULL,
    status connection_status NOT NULL DEFAULT 'pending',
    created_at timestamptz NOT NULL DEFAULT now(),
    responded_at timestamptz NULL,
    CHECK (requester_profile_id <> recipient_profile_id),
    UNIQUE (community_id, requester_profile_id, recipient_profile_id)
);

CREATE INDEX ix_profiles_community_institution_team ON profiles (community_id, institution_id, available_for_team)
    WHERE visible_in_directory = true;
CREATE INDEX ix_profiles_course_trgm ON profiles USING gin (course gin_trgm_ops);
CREATE INDEX ix_skills_name_trgm ON skills USING gin (name gin_trgm_ops);
CREATE INDEX ix_connection_requests_recipient_status ON connection_requests (recipient_profile_id, status);
CREATE INDEX ix_community_invitations_community_email ON community_invitations (community_id, email);
