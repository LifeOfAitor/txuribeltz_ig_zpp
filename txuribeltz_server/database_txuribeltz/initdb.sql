CREATE TYPE erabiltzailemota AS ENUM ('admin', 'user', 'ezabatuta');

CREATE TABLE erabiltzaileak (
    id bigint primary key generated always as identity,
    username text NOT NULL UNIQUE,
    password text NOT NULL,
    elo integer DEFAULT 1000,
    mota erabiltzailemota NOT NULL,
    avatar text
);

CREATE TABLE partidak (
    id bigint primary key generated always as identity,
    player1_id bigint NOT NULL DEFAULT 2,
    player2_id bigint NOT NULL DEFAULT 2,
    winner_id bigint NOT NULL DEFAULT 2,
    played_at date  NOT NULL,
    FOREIGN KEY (player1_id) REFERENCES erabiltzaileak(id) ON DELETE SET DEFAULT,
    FOREIGN KEY (player2_id) REFERENCES erabiltzaileak(id) ON DELETE SET DEFAULT,
    FOREIGN KEY (winner_id) REFERENCES erabiltzaileak(id) ON DELETE SET DEFAULT
);

CREATE TABLE partida_kola (
    id bigint primary key generated always as identity,
    user_id bigint NOT NULL,
    FOREIGN KEY (user_id) REFERENCES erabiltzaileak(id) ON DELETE SET DEFAULT
);

INSERT INTO erabiltzaileak (username, password, mota) 
VALUES ('admin', 'admin', 'admin');

INSERT INTO erabiltzaileak (username, password, mota) 
VALUES ('ezabatuta', 'ezabatuta', 'ezabatuta');

INSERT INTO erabiltzaileak (username, password, mota, elo) VALUES
('aitor', 'aitor123', 'user', 1200),      -- ID 3
('ikerg', 'ikerG123', 'user', 1100),      -- ID 4
('unai', 'unai123', 'user', 1000),        -- ID 5
('jaime', 'jaime123', 'user', 1300),      -- ID 6
('danel', 'danel123', 'user', 1200),      -- ID 7
('julen', 'julen123', 'user', 1000),      -- ID 8
('xabier', 'xabier123', 'user', 1100),    -- ID 9
('ibai', 'ibai123', 'user', 1200),        -- ID 10
('ivan', 'ivan123', 'user', 1000),        -- ID 11
('iker', 'iker123', 'user', 1100),        -- ID 12
('bittor', 'bittor123', 'user', 1400),    -- ID 13
('mikel', 'mikel123', 'user', 1000),      -- ID 14
('jon', 'jon123', 'user', 800),           -- ID 15
('aritz', 'aritz123', 'user', 1400),      -- ID 16
('extra', 'extra123', 'user', 500);       -- ID 17

-- OHARRA: player IDs (1=admin, 2=ezabatuta, 3=aitor...)
INSERT INTO partidak (player1_id, player2_id, winner_id, played_at) VALUES
(3, 4, 3, current_date - 3),      -- aitor vs ikerg, winner: aitor
(5, 6, 6, current_date - 5),      -- unai vs jaime, winner: jaime
(7, 8, 7, current_date - 7),      -- danel vs julen, winner: danel
(9, 10, 10, current_date - 9),    -- xabier vs ibai, winner: ibai
(11, 12, 11, current_date - 11),  -- ivan vs iker, winner: ivan
(13, 14, 14, current_date - 13),  -- bittor vs mikel, winner: mikel
(15, 16, 15, current_date - 15),  -- jon vs aritz, winner: jon
(17, 3, 17, current_date - 18),   -- extra vs aitor, winner: extra
(4, 5, 5, current_date - 20),     -- ikerg vs unai, winner: unai
(6, 7, 6, current_date - 22),     -- jaime vs danel, winner: jaime
(8, 9, 8, current_date - 25),     -- julen vs xabier, winner: julen
(10, 11, 10, current_date - 28),  -- ibai vs ivan, winner: ibai
(12, 13, 13, current_date - 30),  -- iker vs bittor, winner: bittor
(14, 15, 15, current_date - 33),  -- mikel vs jon, winner: jon
(16, 17, 16, current_date - 36),  -- aritz vs extra, winner: aritz
(3, 6, 3, current_date - 40),     -- aitor vs jaime, winner: aitor
(4, 7, 7, current_date - 45),     -- ikerg vs danel, winner: danel
(5, 8, 5, current_date - 50),     -- unai vs julen, winner: unai
(9, 12, 12, current_date - 55),   -- xabier vs iker, winner: iker
(11, 14, 11, current_date - 58);  -- ivan vs mikel, winner: ivan