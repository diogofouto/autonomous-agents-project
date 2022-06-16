using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoleCatcher : Catcher
{    
    public enum ROLE
    {
        OVERTAKING_END_LEFT,
        OVERTAKING_END_RIGHT,
        TYING_END_LEFT,
        TYING_END_RIGHT,
        FOLLOWER
    }

    public static bool[] isRoleAssigned = {false, false, false, false, false};

    private ROLE role = ROLE.FOLLOWER;

    private bool wasAssigned = true;

    private void SetRole(ROLE role)
    {
        this.wasAssigned = true;
        this.role = role;
        isRoleAssigned[(int) role] = true;
    }

    public void UnassignRole()
    {

        if (this.isEndpoint)
        {
            isRoleAssigned[(int) this.role] = false;
            this.wasAssigned = false;
        }
    }

    public bool WasAssigned()
    {
        return this.wasAssigned;
    }

//    private void AssignRoleToEndpoint()
//    {   
//        Vector3 catcherPosition = this.transform.position;
//        Vector3 targetPosition = target.transform.position;
//        Vector3 relativePos = targetPosition - catcherPosition;
//
//        if (Vector3.Dot(target.transform.up, relativePos) > 0) // if it's in front of the target
//        {
//            if (Vector3.SignedAngle(target.transform.up, catcherPosition, -Vector3.forward) > 0 &&
//                !isRoleAssigned[(int) ROLE.TYING_END_LEFT])
//            {
//                SetRole(ROLE.TYING_END_LEFT);
//            }
//            else
//            {
//                SetRole(ROLE.TYING_END_RIGHT);
//            }
//        }
//        else
//        {
//            if (Vector3.SignedAngle(target.transform.up, catcherPosition, -Vector3.forward) > 0 &&
//                !isRoleAssigned[(int) ROLE.OVERTAKING_END_LEFT])
//            {
//                SetRole(ROLE.OVERTAKING_END_LEFT);
//            }
//            else
//            {
//                SetRole(ROLE.OVERTAKING_END_RIGHT);
//            }
//
//        }
//    }

    private void AssignRoleToEndpoint()
    {   
        Vector3 catcherPosition = this.transform.position;
        Vector3 targetPosition = this.target.transform.position;

        Vector3 projection = Vector3.Project(catcherPosition, targetPosition);
        float upPosDotProd = Vector3.Dot(this.transform.up, targetPosition);

        float behindMagnitude;
        if (upPosDotProd >= 0)
        {
            behindMagnitude = Vector3.Magnitude(targetPosition) * 1.15f;
        }
        else
        {
            behindMagnitude = Vector3.Magnitude(targetPosition) * 0.85f;
        }

        if (behindMagnitude < Vector3.Magnitude(projection))
        {
            if (Vector3.SignedAngle(targetPosition, catcherPosition, Vector3.forward) < 0 &&
                !isRoleAssigned[(int) ROLE.TYING_END_LEFT])
            {
                SetRole(ROLE.TYING_END_LEFT);
            }
            else
            {
                SetRole(ROLE.TYING_END_RIGHT);
            }
        }
        else
        {
            if (Vector3.SignedAngle(targetPosition, catcherPosition, Vector3.forward) < 0 &&
                !isRoleAssigned[(int) ROLE.OVERTAKING_END_LEFT])
            {
                SetRole(ROLE.OVERTAKING_END_LEFT);
            }
            else
            {
                SetRole(ROLE.OVERTAKING_END_RIGHT);
            }
        }
    }

    public void AssignRole()
    {
        this.UnassignRole();

        if (this.isEndpoint)
        {
            AssignRoleToEndpoint();
        }
    }

    private Vector3 MoveTowardsOtherEnd()
    {
        List<Catcher> catchers = this.observations.GetCatcherList();
        if (this.index == catchers.Count - 1)
        {
            Debug.DrawRay(this.transform.position, catchers[0].transform.position - this.transform.position, Color.blue);
            return catchers[0].transform.position - this.transform.position;
        }

        Debug.DrawRay(this.transform.position, catchers[catchers.Count - 1].transform.position - this.transform.position, Color.blue);
        return catchers[catchers.Count - 1].transform.position - this.transform.position;
    }

    private Vector3 OvertakeFleer()
    {
        Vector3 catcherPosition = this.transform.position;
        Vector3 targetPosition = this.target.transform.position;

        Vector3 dir = 1.2f * (targetPosition - catcherPosition);

        Vector3 vectorPerpendicularToFleer;

        if (this.role == ROLE.OVERTAKING_END_LEFT)
        {
            vectorPerpendicularToFleer =  2f * Vector3.Cross(targetPosition, Vector3.forward).normalized;
        }
        else
        {
            vectorPerpendicularToFleer =  2f * Vector3.Cross(targetPosition, -Vector3.forward).normalized;
        }

        Debug.DrawRay(this.transform.position, dir + vectorPerpendicularToFleer, Color.yellow);

        return dir + vectorPerpendicularToFleer;
    }

    private Vector3 SelectMoveEndpoint()
    {
        if (this.role == ROLE.TYING_END_LEFT || this.role == ROLE.TYING_END_RIGHT)
        {   
            return MoveTowardsOtherEnd();
        }
        else if (this.role == ROLE.OVERTAKING_END_LEFT || this.role == ROLE.OVERTAKING_END_RIGHT)
        {
            return OvertakeFleer();
        }
        else
        {
            return Vector3.zero;
        }
    }

    private Vector3 SelectMoveNonEndpoint()
    {
        Vector3 dir = this.target.transform.position - this.transform.position;
        
        if (Vector3.Distance(this.transform.position, this.target.transform.position) < 1.5f)
        {
            return -dir;
        }
        return dir;
    }
    
    protected override Vector3 SelectMove()
    {
        List<Catcher> catchers = this.observations.GetCatcherList();

        if (this.target == null || this.target.isCaught)
        {
            if (this.index == 0)
            {
                findTarget();
            }
            else
            {
                // otherwise ask 0 to find a target and communicate it to the others
                catchers[0].findTarget();
                for (int i = 1; i < catchers.Count; i++)
                    catchers[i].target = catchers[0].target;
            }
        }

        if (this.isEndpoint)
        {
            return SelectMoveEndpoint();
        }
        else if (this.role == ROLE.FOLLOWER)
        {
            return SelectMoveNonEndpoint();
        }
        else
        {
            return Vector3.zero;
        }
    }
}
