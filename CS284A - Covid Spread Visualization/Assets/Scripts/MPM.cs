using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Random = UnityEngine.Random;

public class MPM : MonoBehaviour
{
    // Public variables
    [SerializeField] int grid_res = 64; // grid_res x grid_res cells (square)
    [SerializeField] GameObject object_to_spawn;

    // Structs
    struct Particle {
        public float2 x; // position
        public float2 v; // velocity
        public float2x2 C; // affine momentum matrix, unused in this file
        public float mass;
    }
    struct Cell {
        public float2 v; // velocity
        public float mass;
    }

    // simulation parameters

    // dt = the time step of our simulation. the stability of your simulation is going to be limited by how much a particle can
    // move in a single time step, and it's a good rule of thumb to choose dt so that no 
    // particle could move more than 1 grid-cell in a single step. (this would lead to particle tunneling, or other very unstable behaviour)
    const float dt = 1.0f;
    const int iterations = (int)(1.0f / dt);
    
    const float gravity = -0.05f;

    // Arrays
    List<GameObject> particle_game_objects;
    NativeArray<Particle> ps;
    NativeArray<Cell> grid;

    // Priv Vars
    private int num_cells;
    private int num_particles;
    private float2[] weights = new float2[3];

    // interaction
    const float mouse_radius = 10;
    bool mouse_down = false;
    float2 mouse_pos;

    // Start is called before the first frame update
    void Start()
    {
        num_cells = grid_res*grid_res;

        // 1.  initialise your grid - fill your grid array with (grid_res * grid_res) cells.
        grid = new NativeArray<Cell>(num_cells, Allocator.Persistent);
        for(int i = 0; i < num_cells; i++) {
            grid[i] = new Cell();
        }
        
        // 2. create a bunch of particles. set their positions somewhere in your simulation domain.
        // initialise their deformation gradients to the identity matrix, as they're in their undeformed state.

        // initialising a bunch of points in a square
        List<float2> temp_positions = new List<float2>();
        const float spacing = 1.0f;
        const int box_x = 16, box_y = 16;
        float sx = grid_res / 2.0f, sy = grid_res / 2.0f;
        for (float i = sx - box_x / 2; i < sx + box_x / 2; i += spacing) {
            for (float j = sy - box_y / 2; j < sy + box_y / 2; j += spacing) {
                var pos = math.float2(i, j);
                temp_positions.Add(pos);
            }
        }
        num_particles = temp_positions.Count;

        // populate our array of particles, set their initial state
        particle_game_objects = new List<GameObject>();
        ps = new NativeArray<Particle>(num_particles, Allocator.Persistent);
        for (int i = 0; i < num_particles; ++i) {
            // particle
            Particle p = new Particle();
            p.x = temp_positions[i];
            // random initial velocity
            p.v = math.float2(Random.value - 0.5f, Random.value - 0.5f + 2.75f) * 0.5f;
            p.C = 0;
            p.mass = 1.0f;
            ps[i] = p;

            // particle GO
            GameObject p_go = Instantiate(object_to_spawn, new Vector3(p.x.x, this.transform.position.y, p.x.y), Quaternion.identity);
            particle_game_objects.Add(p_go);
        }

        // 3. optionally precompute state variables e.g. particle initial volume, if your model calls for it

    }

    // Update is called once per frame
    void Update()
    {
        HandleMouseInteraction();

        for (int i = 0; i < iterations; ++i) {
            Simulate();
        }
    }

    void HandleMouseInteraction() {
        mouse_down = false;
        if (Input.GetMouseButton(0)) {
            mouse_down = true;
            var mp = Camera.main.ScreenToViewportPoint(Input.mousePosition);
            mouse_pos = math.float2(mp.x * grid_res, mp.y * grid_res);
        }
    }

    void Simulate() {
        // 1. reset our scratch-pad grid completely
        for (int i = 0; i < num_cells; ++i) {
            Cell cell = grid[i];

            cell.mass = 0;
            cell.v = 0;

            grid[i] = cell;
        }

        // 2. particle-to-grid (P2G). 
        // goal: transfers data from particles to our grid
        for (int i = 0; i < num_particles; ++i) {
            var p = ps[i];

            // quadratic interpolation weights
            uint2 cell_idx = (uint2)p.x;
            float2 cell_diff = (p.x - cell_idx) - 0.5f;
            weights[0] = 0.5f * math.pow(0.5f - cell_diff, 2);
            weights[1] = 0.75f - math.pow(cell_diff, 2);
            weights[2] = 0.5f * math.pow(0.5f + cell_diff, 2);

            // for all surrounding 9 cells
            for (uint gx = 0; gx < 3; ++gx) {
                for (uint gy = 0; gy < 3; ++gy) {
                    float weight = weights[gx].x * weights[gy].y;

                    uint2 cell_x = math.uint2(cell_idx.x + gx - 1, cell_idx.y + gy - 1);
                    float2 cell_dist = (cell_x - p.x) + 0.5f;
                    float2 Q = math.mul(p.C, cell_dist);

                    // MPM course, equation 172
                    float mass_contrib = weight * p.mass;

                    // converting 2D index to 1D
                    int cell_index = (int)cell_x.x * grid_res + (int)cell_x.y;
                    Cell cell = grid[cell_index];

                    // scatter mass to the grid
                    cell.mass += mass_contrib;
                    
                    cell.v += mass_contrib * (p.v + Q);
                    grid[cell_index] = cell;
                    
                    // note: currently "cell.v" refers to MOMENTUM, not velocity!
                    // this gets corrected in the UpdateGrid step below.
                }
            }
        }
        
        // 3. calculate grid velocities
        for (int i = 0; i < num_cells; ++i) {
            var cell = grid[i];

            if (cell.mass > 0) {
                // convert momentum to velocity, apply gravity
                cell.v /= cell.mass;
                cell.v += dt * math.float2(0, gravity);

                // boundary conditions
                int x = i / grid_res;
                int y = i % grid_res;
                if (x < 2 || x > grid_res - 3) { cell.v.x = 0; }
                if (y < 2 || y > grid_res - 3) { cell.v.y = 0; }

                grid[i] = cell;
            }

            grid[i] = cell;
        }
        
        // 4. grid-to-particle (G2P). 
        // goal: report our grid's findings back to our particles, and integrate their position + velocity forward
        for (int i = 0; i < num_particles; ++i) {
            var p = ps[i];

            // reset particle velocity. we calculate it from scratch each step using the grid
            p.v = 0;

            // quadratic interpolation weights
            uint2 cell_idx = (uint2)p.x;
            float2 cell_diff = (p.x - cell_idx) - 0.5f;
            weights[0] = 0.5f * math.pow(0.5f - cell_diff, 2);
            weights[1] = 0.75f - math.pow(cell_diff, 2);
            weights[2] = 0.5f * math.pow(0.5f + cell_diff, 2);
            
            // constructing affine per-particle momentum matrix from APIC / MLS-MPM.
            // see APIC paper (https://web.archive.org/web/20190427165435/https://www.math.ucla.edu/~jteran/papers/JSSTS15.pdf), page 6
            // below equation 11 for clarification. this is calculating C = B * (D^-1) for APIC equation 8,
            // where B is calculated in the inner loop at (D^-1) = 4 is a constant when using quadratic interpolation functions
            float2x2 B = 0;
            for (uint gx = 0; gx < 3; ++gx) {
                for (uint gy = 0; gy < 3; ++gy) {
                    float weight = weights[gx].x * weights[gy].y;

                    uint2 cell_x = math.uint2(cell_idx.x + gx - 1, cell_idx.y + gy - 1);
                    int cell_index = (int)cell_x.x * grid_res + (int)cell_x.y;
                    
                    float2 dist = (cell_x - p.x) + 0.5f;
                    float2 weighted_velocity = grid[cell_index].v * weight;

                    // APIC paper equation 10, constructing inner term for B
                    var term = math.float2x2(weighted_velocity * dist.x, weighted_velocity * dist.y);

                    B += term;

                    p.v += weighted_velocity;
                }
            }
            p.C = B * 4;

            // advect particles
            p.x += p.v * dt;

            // safety clamp to ensure particles don't exit simulation domain
            p.x = math.clamp(p.x, 1, grid_res - 2);
            
            // mouse interaction
            if (mouse_down) {
                var dist = p.x - mouse_pos;
                if (math.dot(dist, dist) < mouse_radius * mouse_radius) {
                    float norm_factor = (math.length(dist) / mouse_radius);
                    norm_factor = math.pow(math.sqrt(norm_factor), 8);
                    var force = math.normalize(dist) * norm_factor * 0.5f;
                    p.v += force;
                }
            }

            ps[i] = p;

            // update particle game objects
            particle_game_objects[i].transform.position = new Vector3(p.x.x, this.transform.position.y, p.x.y);
        }
    }

    private void OnDestroy() {
        ps.Dispose();
        grid.Dispose();
    }

}
