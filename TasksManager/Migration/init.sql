CREATE TABLE users (
                       id SERIAL PRIMARY KEY,
                       name VARCHAR(255) NOT NULL,
                       email VARCHAR(255) UNIQUE NOT NULL,
                       password VARCHAR(255) NOT NULL,
                       photo VARCHAR(255)
);

CREATE TABLE projects (
                          id SERIAL PRIMARY KEY,
                          title VARCHAR(255) NOT NULL,
                          description TEXT,
                          owner_id INT NOT NULL,
                          FOREIGN KEY (owner_id) REFERENCES users(id) ON DELETE CASCADE
);

CREATE TABLE project_user (
                              id SERIAL PRIMARY KEY,
                              project_id INT NOT NULL,
                              user_id INT NOT NULL,
                              FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE,
                              FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
);

CREATE TABLE task_statuses (
                               id SERIAL PRIMARY KEY,
                               project_id INT NOT NULL,
                               name VARCHAR(255) NOT NULL,
                               FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE
);

CREATE TABLE tasks (
                       id SERIAL PRIMARY KEY,
                       title VARCHAR(255) NOT NULL,
                       description TEXT NOT NULL,
                       project_id INT NOT NULL,
                       status_id INT NOT NULL,
                       deadline TIMESTAMP,
                       created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                       FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE,
                       FOREIGN KEY (status_id) REFERENCES task_statuses(id) ON DELETE CASCADE
);

CREATE TABLE task_user (
                           id SERIAL PRIMARY KEY,
                           task_id INT NOT NULL,
                           user_id INT NOT NULL,
                           FOREIGN KEY (task_id) REFERENCES tasks(id) ON DELETE CASCADE,
                           FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
);

SELECT u.id, u.name, u.email
FROM users u
         JOIN task_user tu ON u.id = tu.user_id
WHERE tu.task_id = 1;

CREATE TABLE task_files (
                            id SERIAL PRIMARY KEY,
                            task_id INT NOT NULL,
                            file_path VARCHAR(255) NOT NULL,
                            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                            updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                            FOREIGN KEY (task_id) REFERENCES tasks(id) ON DELETE CASCADE
);

CREATE TABLE task_comments (
                               id SERIAL PRIMARY KEY,
                               task_id INT NOT NULL,
                               user_id INT NOT NULL,
                               comment TEXT NOT NULL,
                               created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                               updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                               FOREIGN KEY (task_id) REFERENCES tasks(id) ON DELETE CASCADE,
                               FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
);

CREATE TABLE checklists (
                            id SERIAL PRIMARY KEY,
                            task_id INT NOT NULL,
                            title VARCHAR(255) NOT NULL,
                            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                            updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                            FOREIGN KEY (task_id) REFERENCES tasks(id) ON DELETE CASCADE
);

CREATE TABLE checklist_items (
                                 id SERIAL PRIMARY KEY,
                                 checklist_id INT NOT NULL,
                                 content VARCHAR(255) NOT NULL,
                                 is_completed BOOLEAN DEFAULT FALSE,
                                 created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                                 updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                                 FOREIGN KEY (checklist_id) REFERENCES checklists(id) ON DELETE CASCADE
);
